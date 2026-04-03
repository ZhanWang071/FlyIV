import json
import os
import glob
from datetime import datetime

# Task categorization
# TASK_CATEGORIES = {
#     # data-level tasks (1-22)
#     'data_level': {
#         'DATA_TRANSFORM': [22],
#         'UPDATE': [7],
#         'DELETE_ELEMENT': [8],
#         'APPEND_SINGLE': [9],
#         'APPEND_SERIES': [10],
#     },
#     # Vis-level tasks (1-22)
#     'vis_level': {
#         'CREATE': [1, 2, 3, 4, 5],
#         'DELETE': [6],
#         'ROTATE': [15],
#         'SCALE': [16],
#         'LAYOUT': [20, 21],
#         'POSITION': [14],
#         'HIGHLIGHT': [11],
#         'CHANGE_SERIE_COLOR': [12],
#         'CHANGE_DATA_COLOR': [13]
#     },
#     # Context-level tasks (1-22)
#     'env_level': {
#         'ORIENT_TO': [17],
#         'ADAPT_POS': [18],
#         'EMBED': [19],
#     },
#     # Combined tasks (81-100)
#     'combined': {
#         'COMBINED': [23, 24, 25, 26, 27, 28, 29, 30]
#     }
# }
TASK_CATEGORIES = {
    'data_level': {
        'DATA_TRANSFORM': [1, 2, 3, 4, 5],
        'UPDATE': [6, 7, 8, 9, 10],
        'DELETE_ELEMENT': [11, 12, 13, 14, 15],
        'APPEND_SINGLE': [16, 17, 18, 19, 20],
        'APPEND_SERIES': [21, 22, 23, 24, 25],
    },
    'vis_level': {
        'CREATE': [26, 27, 28, 29, 30],
        'DELETE': [31, 32, 33, 34, 35],
        'POSITION': [36, 37, 38, 39, 40],
        'ROTATE': [41, 42, 43, 44, 45],
        'SCALE': [46, 47, 48, 49, 50],
        'LAYOUT': [51, 52, 53, 54, 55],
        'CHANGE_SERIE_COLOR': [56, 57, 58, 59, 60],
        'CHANGE_DATA_COLOR': [61, 62, 63, 64, 65],
    },
    'env_level': {
        'ORIENT_TO': [66, 67, 68, 69, 70],
        'ADAPT_POS': [71, 72, 73, 74, 75],
        'EMBED': [76, 77, 78, 79, 80],
    },
    'combined': {
        'COMBINED': list(range(81, 101)),  # 81–100
    }
}
def get_task_category(test_id):
    """Determine which category a test belongs to"""
    for category, operations in TASK_CATEGORIES.items():
        for op, ids in operations.items():
            if test_id in ids:
                return category, op
    return None, None

def is_message_output(generated_code):
    """Check if the output is a MESSAGE (indicating failure)"""
    if not generated_code:
        return True
    return generated_code.strip().startswith('MESSAGE(')

def analyze_json_file(json_path):
    """Analyze a single JSON log file"""
    # with open(json_path, 'r', encoding='utf-8') as f:
    with open(json_path, 'r', encoding='utf-8-sig') as f:
        data = json.load(f)
    
    # Initialize statistics
    stats = {
        'total': {'count': 0, 'success': 0, 'failed': 0, 'llm_time': [], 'exec_time': [], 'total_time': []},
        'single_task': {'count': 0, 'success': 0, 'failed': 0, 'llm_time': [], 'exec_time': [], 'total_time': []},
        'combined_task': {'count': 0, 'success': 0, 'failed': 0, 'llm_time': [], 'exec_time': [], 'total_time': []},
        'env_level': {'count': 0, 'success': 0, 'failed': 0, 'llm_time': [], 'exec_time': [], 'total_time': []},
        'vis_level': {'count': 0, 'success': 0, 'failed': 0, 'llm_time': [], 'exec_time': [], 'total_time': []},
        'data_level': {'count': 0, 'success': 0, 'failed': 0, 'llm_time': [], 'exec_time': [], 'total_time': []}
    }
    
    outliers = []
    # Process each test
    for test in data:
        test_id = test['test_id']
        if test['llm_time'] > 30: 
            outliers.append(test_id)
            continue  # Skip outliers for LLM time
        category, operation = get_task_category(test_id)
        
        # Check if it's a MESSAGE output (failure)
        is_failed = is_message_output(test.get('generated_code', ''))
        
        # Update total stats
        stats['total']['count'] += 1
        if is_failed:
            stats['total']['failed'] += 1
        elif test['success']:
            stats['total']['success'] += 1
        else:
            stats['total']['failed'] += 1
        stats['total']['llm_time'].append(test.get('llm_time', 0))
        stats['total']['exec_time'].append(test.get('execution_time', 0))
        stats['total']['total_time'].append(test.get('total_time', 0))
        
        # Update single/combined task stats
        if test_id <= 80:
            task_type = 'single_task'
        else:
            task_type = 'combined_task'
        
        stats[task_type]['count'] += 1
        if is_failed or not test['success']:
            stats[task_type]['failed'] += 1
        else:
            stats[task_type]['success'] += 1
        stats[task_type]['llm_time'].append(test.get('llm_time', 0))
        stats[task_type]['exec_time'].append(test.get('execution_time', 0))
        stats[task_type]['total_time'].append(test.get('total_time', 0))
        
        # Update category-specific stats (only for single tasks 1-80)
        if category and test_id <= 80 and category in ['env_level', 'vis_level', 'data_level']:
            stats[category]['count'] += 1
            if is_failed or not test['success']:
                stats[category]['failed'] += 1
            else:
                stats[category]['success'] += 1
            stats[category]['llm_time'].append(test.get('llm_time', 0))
            stats[category]['exec_time'].append(test.get('execution_time', 0))
            stats[category]['total_time'].append(test.get('total_time', 0))
    
    return stats, outliers

def calculate_averages(time_list):
    """Calculate average from a list of times"""
    if not time_list:
        return 0.0
    return sum(time_list) / len(time_list)

def format_stats(stats_dict):
    """Format statistics for a category"""
    if stats_dict['count'] == 0:
        return "N/A", "N/A", "N/A", "N/A"
    
    success_rate = (stats_dict['success'] / stats_dict['count']) * 100
    avg_llm = calculate_averages(stats_dict['llm_time'])
    avg_exec = calculate_averages(stats_dict['exec_time'])
    avg_total = calculate_averages(stats_dict['total_time'])
    
    return f"{success_rate:.1f}%", f"{avg_llm:.3f}s", f"{avg_exec:.3f}s", f"{avg_total:.3f}s"

def generate_report(json_path, stats):
    """Generate formatted report"""
    filename = os.path.basename(json_path)
    report_lines = []
    
    report_lines.append("=" * 80)
    report_lines.append(f"EVALUATION SUMMARY: {filename}")
    report_lines.append("=" * 80)
    report_lines.append("")
    
    # Overall statistics
    report_lines.append("OVERALL STATISTICS")
    report_lines.append("-" * 80)
    sr, llm, exe, tot = format_stats(stats['total'])
    report_lines.append(f"Total Tests: {stats['total']['count']}")
    report_lines.append(f"Success Rate: {sr}")
    report_lines.append(f"Avg LLM Time: {llm}")
    report_lines.append(f"Avg Execution Time: {exe}")
    report_lines.append(f"Avg Total Time: {tot}")
    report_lines.append("")
    
    # Single vs Combined tasks
    report_lines.append("TASK TYPE BREAKDOWN")
    report_lines.append("-" * 80)
    
    report_lines.append(f"\nSingle Tasks (1-22):")
    sr, llm, exe, tot = format_stats(stats['single_task'])
    report_lines.append(f"  Count: {stats['single_task']['count']}")
    report_lines.append(f"  Success Rate: {sr}")
    report_lines.append(f"  Avg LLM Time: {llm}")
    report_lines.append(f"  Avg Execution Time: {exe}")
    report_lines.append(f"  Avg Total Time: {tot}")
    
    report_lines.append(f"\nCombined Tasks (23-30):")
    sr, llm, exe, tot = format_stats(stats['combined_task'])
    report_lines.append(f"  Count: {stats['combined_task']['count']}")
    report_lines.append(f"  Success Rate: {sr}")
    report_lines.append(f"  Avg LLM Time: {llm}")
    report_lines.append(f"  Avg Execution Time: {exe}")
    report_lines.append(f"  Avg Total Time: {tot}")
    report_lines.append("")
    
    # Category breakdown (env, vis, data level)
    report_lines.append("SINGLE TASK CATEGORY BREAKDOWN")
    report_lines.append("-" * 80)
    
    for category_name, display_name in [
        ('env_level', 'Environment-Level Operations'),
        ('vis_level', 'Visualization-Level Operations'),
        ('data_level', 'Data-Level Operations')
    ]:
        category_stats = stats[category_name]
        if category_stats['count'] > 0:
            sr, llm, exe, tot = format_stats(category_stats)
            report_lines.append(f"\n{display_name}:")
            report_lines.append(f"  Count: {category_stats['count']}")
            report_lines.append(f"  Success Rate: {sr}")
            report_lines.append(f"  Avg LLM Time: {llm}")
            report_lines.append(f"  Avg Execution Time: {exe}")
            report_lines.append(f"  Avg Total Time: {tot}")
    
    report_lines.append("")
    report_lines.append("=" * 80)
    report_lines.append(f"Report generated at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    report_lines.append("=" * 80)
    
    return "\n".join(report_lines)

def main():
    # Find all JSON files in the Logs/Test directory
    log_dir = os.path.join(os.path.dirname(__file__), '..', 'Client', 'Assets', 'Logs', 'Test', 'v3')
    json_files = glob.glob(os.path.join(log_dir, '*.json'))

    outliers = []
    
    if not json_files:
        print("No evaluation JSON files found in Client/Assets/Logs/Test/v3/")
        return
    
    print(f"Found {len(json_files)} JSON file(s) to analyze...")
    
    for json_path in json_files:
        print(f"\nAnalyzing: {os.path.basename(json_path)}")
        
        try:
            # Analyze the JSON file
            stats, outliers = analyze_json_file(json_path)
            
            # Generate report
            report = generate_report(json_path, stats)
            
            # Print to console
            print(report)
            
            # Save to txt file in Server directory
            server_dir = os.path.dirname(__file__)
            txt_filename = os.path.basename(json_path).replace('.json', '_summary.txt')
            txt_path = os.path.join(server_dir, "TestResults_v3", txt_filename)
            with open(txt_path, 'w', encoding='utf-8') as f:
                f.write(report)
            
            print(f"\nSummary saved to: {txt_path}")
            print(f"Outliers (LLM time > 10s) in {os.path.basename(json_path)}: {outliers}")
            
        except Exception as e:
            print(f"Error analyzing {json_path}: {str(e)}")
            import traceback
            traceback.print_exc()

if __name__ == "__main__":
    main()
