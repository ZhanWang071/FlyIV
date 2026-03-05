using System.Runtime.CompilerServices;

// Genies - same package
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Editor")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Sample")]

// com.genies.sdk.avatar.telemetry
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Telemetry")]
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Telemetry.Editor")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
// com.genies.experience.core
[assembly: InternalsVisibleTo("DOTween.Modules.generated")]
[assembly: InternalsVisibleTo("DOTween.generated")]
[assembly: InternalsVisibleTo("Genies.Experience.Core")]
[assembly: InternalsVisibleTo("Genies.Experience.Core.generated")]
[assembly: InternalsVisibleTo("Genies.Experience.Shared.Bindings.generated")]
[assembly: InternalsVisibleTo("Genies.Naf.generated")]
[assembly: InternalsVisibleTo("Genies.Scripting.Bindings.generated")]
[assembly: InternalsVisibleTo("Genies.Sdk.Core.generated")]
[assembly: InternalsVisibleTo("LeanCommon.generated")]
[assembly: InternalsVisibleTo("LeanTouch.generated")]
[assembly: InternalsVisibleTo("UniTask.DOTween.generated")]
[assembly: InternalsVisibleTo("UniTask.generated")]
[assembly: InternalsVisibleTo("Unity.TextMeshPro.generated")]
[assembly: InternalsVisibleTo("UnityEngine.AIModule.generated")]
[assembly: InternalsVisibleTo("UnityEngine.AnimationModule.generated")]
[assembly: InternalsVisibleTo("UnityEngine.AudioModule.generated")]
[assembly: InternalsVisibleTo("UnityEngine.CoreModule.generated")]
[assembly: InternalsVisibleTo("UnityEngine.IMGUIModule.generated")]
[assembly: InternalsVisibleTo("UnityEngine.InputLegacyModule.generated")]
[assembly: InternalsVisibleTo("UnityEngine.ParticleSystemModule.generated")]
[assembly: InternalsVisibleTo("UnityEngine.Physics2DModule.generated")]
[assembly: InternalsVisibleTo("UnityEngine.PhysicsModule.generated")]
[assembly: InternalsVisibleTo("UnityEngine.TerrainModule.generated")]
[assembly: InternalsVisibleTo("UnityEngine.TerrainPhysicsModule.generated")]
[assembly: InternalsVisibleTo("UnityEngine.UI.generated")]
[assembly: InternalsVisibleTo("UnityEngine.UIModule.generated")]
[assembly: InternalsVisibleTo("UnityEngine.VehiclesModule.generated")]
[assembly: InternalsVisibleTo("UnityEngine.WindModule.generated")]
[assembly: InternalsVisibleTo("mscorlib.generated")]
// com.genies.experience.gameplay
[assembly: InternalsVisibleTo("Genies.Experience.Gameplay")]
[assembly: InternalsVisibleTo("Genies.Experience.Gameplay.Editor")]
[assembly: InternalsVisibleTo("SampleGeniesAvatarController")]
[assembly: InternalsVisibleTo("SampleShooter")]
[assembly: InternalsVisibleTo("SmapleBaseball")]
// com.genies.multiplayer
[assembly: InternalsVisibleTo("Genies.Multiplayer")]
[assembly: InternalsVisibleTo("Genies.Multiplayer.Editor")]
// com.genies.sdk.avatar
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar")]
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Editor")]
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Samples.AvatarStarter")]
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Samples.DebugSdkFunctions")]
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Samples.MultipleAvatars")]
// com.genies.sdk.core
[assembly: InternalsVisibleTo("Genies.Sdk.Core")]
[assembly: InternalsVisibleTo("Genies.Sdk.Core.Editor")]
#endif
