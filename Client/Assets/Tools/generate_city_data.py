#!/usr/bin/env python3
"""
生成 FlyIV city 场景的建筑数据（18 栋楼 + T5/T6 使用的合并文件）。

设计：四类建筑原型（住宅/办公/工业/商业），每栋楼有自己的类型、规模系数
和可选的峰值变形，最终叠加 4% 的随机抖动（固定种子，保证可复现）。

用法：
    python3 Tools/generate_city_data.py

输出：
    StreamingAssets/DxRData/city/building_001~018.json
    StreamingAssets/DxRData/city/city_all.json
    StreamingAssets/DxRData/city/city_electricity.json
"""

import json
import random
import os

ASSETS = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CITY_DIR = os.path.join(ASSETS, "StreamingAssets", "DxRData", "city")

TIMES = ["00:00", "04:00", "08:00", "12:00", "16:00", "20:00"]

# 四种建筑类型的基础曲线（electricity, water, gas, footfall），按 TIMES 顺序
ARCHETYPES = {
    "residential": {  # 住宅：早晚双峰，傍晚用电最高
        "electricity": [130, 110, 420, 620, 1450, 1750],
        "water":       [22,  16,  140, 380, 520, 640],
        "gas":         [8,   6,   45,  120, 190, 240],
        "footfall":    [15,  5,   120, 260, 420, 380],
    },
    "office": {       # 办公：白天工作时段高，夜间很低
        "electricity": [70,  55,  1050, 1900, 1750, 550],
        "water":       [12,  8,   210,  420,  380,  160],
        "gas":         [3,   2,   60,   110,  95,   30],
        "footfall":    [5,   2,   320,  620,  560,  140],
    },
    "industrial": {   # 工业：全天高位，午间最高
        "electricity": [950, 1150, 1750, 2500, 2300, 1750],
        "water":       [180, 220, 420,  700,  640,  460],
        "gas":         [60,  75,  160,  320,  280,  190],
        "footfall":    [80,  60,  180,  300,  280,  160],
    },
    "mall": {         # 商业/娱乐：傍晚到夜间最高，人流最大
        "electricity": [80,  60,  500, 1400, 2300, 2650],
        "water":       [15,  10,  130, 380,  620,  720],
        "gas":         [4,   3,   40,  110,  160,  180],
        "footfall":    [10,  5,   200, 620,  980,  1100],
    },
}

# 每栋楼的类型、规模系数、峰值变形（None / "peak16" / "night" / "late"）
BUILDINGS = {
    1:  ("residential", 1.00, None),
    2:  ("residential", 0.82, None),
    3:  ("residential", 1.15, None),
    4:  ("residential", 0.70, None),
    5:  ("office",      1.20, None),   # 005 改为大型办公楼
    6:  ("office",      0.90, None),
    7:  ("office",      1.10, None),
    8:  ("office",      0.80, None),
    9:  ("office",      1.20, None),
    10: ("office",      0.95, "peak16"),   # 特例：16:00 峰值
    11: ("industrial",  0.85, None),
    12: ("industrial",  1.05, None),
    13: ("industrial",  1.20, None),
    14: ("industrial",  0.80, None),
    15: ("industrial",  1.30, "night"),    # 特例：夜班，04:00 峰值
    16: ("mall",        0.90, None),
    17: ("mall",        1.10, None),
    18: ("mall",        1.00, "late"),     # 特例：20:00 峰值更高
}

RNG = random.Random(20260825)


def build_series(base, scale, variant, key, idx):
    vals = [round(v * scale) for v in base]
    if variant == "peak16" and key == "electricity":
        # 峰值从 12:00 移到 16:00
        vals[3], vals[4] = round(base[4] * scale), round(base[3] * scale)
    elif variant == "night" and key in ("electricity", "water", "gas"):
        # 夜班：04:00 为最高点（把 12:00 与 04:00 对调）
        vals[1], vals[3] = round(base[3] * scale), round(base[1] * scale)
    elif variant == "late" and key in ("electricity", "footfall"):
        # 更晚的峰值：20:00 再抬高
        vals[5] = round(base[5] * scale * 1.15)
    # 轻微随机抖动 ±4%，让同类型建筑也有差异
    vals = [max(0, round(v * (1 + RNG.uniform(-0.04, 0.04)))) for v in vals]
    return vals


def main():
    os.makedirs(CITY_DIR, exist_ok=True)

    for num, (atype, scale, variant) in BUILDINGS.items():
        arch = ARCHETYPES[atype]
        rows = []
        for i, t in enumerate(TIMES):
            rows.append({
                "time": t,
                "electricity": build_series(arch["electricity"], scale, variant, "electricity", i)[i],
                "water":       build_series(arch["water"],       scale, variant, "water",       i)[i],
                "gas":         build_series(arch["gas"],         scale, variant, "gas",         i)[i],
                "footfall":    build_series(arch["footfall"],    scale, variant, "footfall",    i)[i],
            })
        path = os.path.join(CITY_DIR, f"building_{num:03d}.json")
        with open(path, "w") as f:
            json.dump(rows, f, indent=2)

    # 合并文件（T5/T6 使用）
    all_rows = []
    for num in range(1, 19):
        rows = json.load(open(os.path.join(CITY_DIR, f"building_{num:03d}.json")))
        for r in rows:
            all_rows.append({"building": f"building_{num:03d}", **r})
    with open(os.path.join(CITY_DIR, "city_all.json"), "w") as f:
        json.dump(all_rows, f, indent=2)
    with open(os.path.join(CITY_DIR, "city_electricity.json"), "w") as f:
        json.dump([{k: r[k] for k in ("building", "time", "electricity")} for r in all_rows], f, indent=2)

    print("生成完成:", len(BUILDINGS), "栋楼 + 2 个合并文件")


if __name__ == "__main__":
    main()
