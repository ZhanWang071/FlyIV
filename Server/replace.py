#!/usr/bin/env python3
import sys
import json

def main():
    if len(sys.argv) != 3:
        print("Usage: python replace.py <a.json> <b.json>")
        sys.exit(1)

    a_path = sys.argv[1]
    b_path = sys.argv[2]

    # 读取 JSON 文件
    try:
        with open(a_path, 'r', encoding='utf-8') as f:
            a_data = json.load(f)
        with open(b_path, 'r', encoding='utf-8') as f:
            b_data = json.load(f)
    except Exception as e:
        print(f"Error reading files: {e}")
        sys.exit(1)

    # 确保输入是列表
    if not isinstance(a_data, list) or not isinstance(b_data, list):
        print("Both files must contain a JSON array at top level.")
        sys.exit(1)

    # 构建 b 的快速查找字典
    b_dict = {item['test_id']: item for item in b_data if 'test_id' in item}

    # 遍历 a_data 并替换符合条件的条目
    updated = False
    for i, a_item in enumerate(a_data):
        test_id = a_item.get('test_id')
        if test_id is None:
            continue
        if test_id in b_dict:
            b_item = b_dict[test_id]
            # 比较 llm_time，若 b 的更小则替换
            if b_item.get('llm_time', float('inf')) < a_item.get('llm_time', float('inf')):
                a_data[i] = b_item
                print(f"Replaced entry for test_id {test_id} with llm_time {b_item.get('llm_time')} (was {a_item.get('llm_time')})")
                updated = True

    # 如果发生过替换，写回 a 文件
    if updated:
        try:
            with open(a_path, 'w', encoding='utf-8') as f:
                json.dump(a_data, f, indent=2, ensure_ascii=False)
            print(f"Updated {a_path} with entries from {b_path} where llm_time was smaller.")
        except Exception as e:
            print(f"Error writing file: {e}")
            sys.exit(1)
    else:
        print("No updates were made.")

if __name__ == '__main__':
    main()