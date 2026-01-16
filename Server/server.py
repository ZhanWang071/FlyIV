from flask import Flask, request, jsonify
from code_generation import generate_functions

app = Flask(__name__)

@app.route('/generate_skill', methods=['POST'])
def handle_request():
    return generate_functions(request.json)

if __name__ == '__main__':
    print("Python 代码生成服务器已启动: http://127.0.0.1:5001")
    app.run(port=5001)