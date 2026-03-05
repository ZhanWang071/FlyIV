using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Services.Model;
using Genies.DataRepositoryFramework;
using Genies.Login.Native;
using static Genies.CrashReporting.CrashReporter;

namespace Genies.CloudSave
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class CloudFeatureSaveService<T> : ICloudFeatureSaveService<T>
#else
    public class CloudFeatureSaveService<T> : ICloudFeatureSaveService<T>
#endif
    {
        public delegate void SetId(T data, string id);

        private readonly IDataRepository<GameFeature> _repository;
        private ICloudSaveJsonSerializer<T> _serializer;
        private readonly SetId _idSetter;
        private readonly Func<T, string> _idGetter;

        /// <summary>
        /// In-memory session cache for anonymous users. Data stored here is not persisted to the cloud
        /// and only lives for the current session, allowing anonymous users to create and view custom
        /// colors within the editor without triggering cloud save operations
        /// </summary>
        private readonly Dictionary<string, T> _anonymousSessionCache = new Dictionary<string, T>();

        public GameFeature.GameFeatureTypeEnum FeatureTypeEnum { get; }

        public CloudFeatureSaveService(GameFeature.GameFeatureTypeEnum featureTypeEnum, ICloudSaveJsonSerializer<T> serializer, SetId idSetter, Func<T, string> idGetter, IDataRepository<GameFeature> repo = null)
        {
            _serializer = serializer;
            _idSetter = idSetter;
            _idGetter = idGetter;
            FeatureTypeEnum = featureTypeEnum;
            if (repo == null) {
                _repository = new CloudSaveApiDataRepository(featureTypeEnum);
            } else {
                _repository = repo;
            }
        }

        private async UniTask<string> GenerateValidGuid()
        {
            var guid       = Guid.NewGuid().ToString();
            var currentIds = await GetIdsAsync();

            while (currentIds.Contains(guid))
            {
                guid = Guid.NewGuid().ToString();
            }

            return guid;
        }

        /// <summary>
        /// Returns the record's ID, generating and assigning one if it is null or empty
        /// </summary>
        private string GetOrCreateLocalId(T data)
        {
            var id = _idGetter(data);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
                _idSetter?.Invoke(data, id);
            }
            return id;
        }

        /// <summary>
        /// Converts a <see cref="GameFeature"/> record to <see cref="T"/> record
        /// </summary>
        /// <param name="gameFeature"> The game feature record </param>
        /// <exception cref="CloudSaveException"> Throw exception if invalid json </exception>
        private T FromGameFeature(GameFeature gameFeature)
        {
            //Can't process invalid json.
            if (!_serializer.IsValidJson(gameFeature.GameFeatureDefinitionJson))
            {
                throw new CloudSaveException($"Invalid Json for type: {FeatureTypeEnum}, Data: {gameFeature.GameFeatureDefinitionJson}");
            }

            var toReturn = _serializer.FromJson(gameFeature.GameFeatureDefinitionJson);
            _idSetter?.Invoke(toReturn, gameFeature.GameFeatureId);
            return toReturn;
        }

        /// <summary>
        /// Converts a <see cref="T"/> record to a <see cref="GameFeature"/> record
        /// </summary>
        /// <param name="dataRecord"> The data record </param>
        /// <exception cref="CloudSaveException"> Throw exception if invalid json </exception>
        private async UniTask<GameFeature> ToGameFeature(T dataRecord)
        {
            try
            {
                var id       = _idGetter(dataRecord);

                if (string.IsNullOrEmpty(id))
                {
                    id = await GenerateValidGuid();
                    _idSetter?.Invoke(dataRecord, id);
                }

                var jsonData = _serializer.ToJson(dataRecord);
                return new GameFeature(id, FeatureTypeEnum, jsonData);
            }
            catch (Exception e)
            {
                throw new CloudSaveException($"Failed to serialize record for: {FeatureTypeEnum}, Id: {_idGetter(dataRecord)}", e);
            }
        }

        public async UniTask<int> GetCountAsync()
        {
            if (GeniesLoginSdk.IsUserSignedInAnonymously())
            {
                return _anonymousSessionCache.Count;
            }

            return await _repository.GetCountAsync();
        }

        public async UniTask<List<string>> GetIdsAsync()
        {
            if (GeniesLoginSdk.IsUserSignedInAnonymously())
            {
                return new List<string>(_anonymousSessionCache.Keys);
            }

            return await _repository.GetIdsAsync();
        }

        public async UniTask<List<T>> GetAllAsync()
        {
            if (GeniesLoginSdk.IsUserSignedInAnonymously())
            {
                return new List<T>(_anonymousSessionCache.Values);
            }

            var gameFeatures = await _repository.GetAllAsync();

            try
            {
                return gameFeatures.Select(FromGameFeature).ToList();
            }
            catch (CloudSaveException cse)
            {
                LogHandledException(cse);
            }
            catch (Exception e)
            {
                LogHandledException(new CloudSaveException($"Failed to deserialize json records for {FeatureTypeEnum}", e));
            }

            return new List<T>();
        }

        public async UniTask<T> GetByIdAsync(string recordId)
        {
            if (GeniesLoginSdk.IsUserSignedInAnonymously())
            {
                _anonymousSessionCache.TryGetValue(recordId, out var cachedRecord);
                return cachedRecord;
            }

            var gameFeature = await _repository.GetByIdAsync(recordId);

            try
            {
                return FromGameFeature(gameFeature);
            }
            catch (CloudSaveException cse)
            {
                LogHandledException(cse);
            }
            catch (Exception e)
            {
                LogHandledException(new CloudSaveException($"Failed to deserialize json record {recordId} for {FeatureTypeEnum}", e));
            }

            return default;
        }

        public async UniTask<T> CreateAsync(T data)
        {
            if (GeniesLoginSdk.IsUserSignedInAnonymously())
            {
                var id = GetOrCreateLocalId(data);
                _anonymousSessionCache[id] = data;
                return data;
            }

            try
            {
                var createBody     = await ToGameFeature(data);
                var createdFeature = await _repository.CreateAsync(createBody);

                var result = FromGameFeature(createdFeature);
                return result;
            }
            catch (CloudSaveException cse)
            {
                LogHandledException(cse);
            }
            catch (Exception e)
            {
                LogHandledException(new CloudSaveException($"Failed to create json record {_idGetter(data)} for {FeatureTypeEnum}", e));
            }

            return default;
        }

        public async UniTask<List<T>> BatchCreateAsync(List<T> data)
        {
            if (GeniesLoginSdk.IsUserSignedInAnonymously())
            {
                foreach (var d in data)
                {
                    var id = GetOrCreateLocalId(d);
                    _anonymousSessionCache[id] = d;
                }
                return new List<T>(data);
            }

            try
            {
                var tasks = new List<UniTask<GameFeature>>();
                foreach (var d in data)
                {
                    tasks.Add(ToGameFeature(d));
                }

                var asGameFeatures = await UniTask.WhenAll(tasks);

                //Save data
                var created = await _repository.BatchCreateAsync(asGameFeatures.ToList());
                return created.Select(FromGameFeature).ToList();
            }
            catch (CloudSaveException cse)
            {
                LogHandledException(cse);
            }
            catch (Exception e)
            {
                LogHandledException(new CloudSaveException($"Failed to create json records for {FeatureTypeEnum}", e));
            }

            return default;
        }

        public async UniTask<T> UpdateAsync(T updatedDataRecord)
        {
            if (GeniesLoginSdk.IsUserSignedInAnonymously())
            {
                var id = GetOrCreateLocalId(updatedDataRecord);
                _anonymousSessionCache[id] = updatedDataRecord;
                return updatedDataRecord;
            }

            try
            {
                var asGameFeature      = await ToGameFeature(updatedDataRecord);
                var updatedGameFeature = await _repository.UpdateAsync(asGameFeature);

                return FromGameFeature(updatedGameFeature);
            }
            catch (CloudSaveException cse)
            {
                LogHandledException(cse);
            }
            catch (Exception e)
            {
                LogHandledException(new CloudSaveException($"Failed to update json record {_idGetter(updatedDataRecord)} for {FeatureTypeEnum}", e));
            }

            return default;
        }

        public async UniTask<List<T>> BatchUpdateAsync(List<T> updatedRecords)
        {
            if (GeniesLoginSdk.IsUserSignedInAnonymously())
            {
                foreach (var d in updatedRecords)
                {
                    var id = GetOrCreateLocalId(d);
                    _anonymousSessionCache[id] = d;
                }
                return new List<T>(updatedRecords);
            }

            try
            {
                var tasks = new List<UniTask<GameFeature>>();
                foreach (var d in updatedRecords)
                {
                    tasks.Add(ToGameFeature(d));
                }

                var asGameFeatures  = await UniTask.WhenAll(tasks);
                var updatedFeatures = await _repository.BatchUpdateAsync(asGameFeatures.ToList());
                return updatedFeatures.Select(FromGameFeature).ToList();
            }
            catch (CloudSaveException cse)
            {
                LogHandledException(cse);
            }
            catch (Exception e)
            {
                LogHandledException(new CloudSaveException($"Failed to update json records for {FeatureTypeEnum}", e));
            }

            return default;
        }

        public async UniTask<bool> DeleteAsync(string id)
        {
            if (GeniesLoginSdk.IsUserSignedInAnonymously())
            {
                if (id != null)
                {
                    return _anonymousSessionCache.Remove(id);
                }

                return true; // No id to remove if null
            }

            return await _repository.DeleteAsync(id);
        }

        public async UniTask<bool> BatchDeleteAsync(List<string> ids)
        {
            if (GeniesLoginSdk.IsUserSignedInAnonymously())
            {
                foreach (var id in ids)
                {
                    if (id != null)
                    {
                        _anonymousSessionCache.Remove(id);
                    }
                }

                return true;
            }

            return await _repository.BatchDeleteAsync(ids);
        }

        public async UniTask<bool> DeleteAllAsync()
        {
            if (GeniesLoginSdk.IsUserSignedInAnonymously())
            {
                _anonymousSessionCache.Clear();
                return true;
            }

            return await _repository.DeleteAllAsync();
        }

        public void SetJsonSerializer(ICloudSaveJsonSerializer<T> serializer)
        {
            _serializer = serializer;
        }
    }
}
