# =======================================
# Generate code for skill function via large models
# =======================================
import re
from flask import request, jsonify
import json
import os

def generate_functions(data):
    response_data = []

    for item in data:
        # 提取函数名和参数，例如 "UPDATE(string element_id, float value)"
        func_sig = item.get("function", "")
        func_description = item.get("description", "")
        print("正在生成函数：" + func_sig + " 功能描述：" + func_description)
        match = re.match(r"(\w+)\((.*)\)", func_sig)
        
        if match:
            func_name = match.group(1) # UPDATE
            params = match.group(2)    # string element_id, float value
            
            # xcharts库代码生成
            generated_code = generate_xcharts_code(func_name, params)
            if generated_code is None:
                # 如果没有返回预定义的完整代码，未来替换成大模型生成
                generated_code = generate_mock_code(func_name, params)
    
    return generated_code
    #         response_data.append({
    #             "function": func_name,
    #             "code": generated_code
    #         })

    # return jsonify(response_data)

def generate_mock_code(func_name, params):
    class_name = transfer_skill_to_class_name(func_name)
    
    code_template = f"""using UnityEngine;

public class {class_name}
{{
    public static void Execute({params})
    {{
        Debug.Log("正在执行动态生成的 {class_name} 逻辑");
    }}
}}"""
    return code_template

# 输入函数名和参数，生成对应的XCharts代码字符串
# 输入样例 (func_name="UPDATE", params="BaseChart chart, string element_id, float value")
# 输入结构化的c#代码字符串，包含类定义和方法实现
def generate_xcharts_code(func_name, params):
    json_path = os.path.join(os.path.dirname(__file__), "xcharts_func.json")
    with open(json_path, "r", encoding="utf-8") as f:
        func_list = json.load(f)
    
    for entry in func_list:
        if entry.get("function", "").upper() == func_name.upper():
            return entry.get("code", "")
    
    return None  # 未找到匹配项


# 转换名称格式用于类名 (例如 UPDATE -> Update)
def transfer_skill_to_class_name(func_name):
    class_name = "".join(word.capitalize() for word in func_name.split("_"))
    return class_name


## 简单测试xcharts 0223
if __name__ == "__main__":
    test_data = [
        {
            "function": "UPDATE(BaseChart chart,string element_id, float value)",
            "description": "Update the mark in a visualization"
        }
    ]
    generated_code = generate_functions(test_data)
    print(generated_code)

