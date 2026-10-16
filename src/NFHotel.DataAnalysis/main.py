import os
import subprocess
import sys

import lmstudio as lms
import pandas as pd

# Constants
DATA_FILE_PATH = "data/nf_hotel_bookings.csv"
AI_PROMT_DESCRIPTIVE_ANALYSIS = """
  You work as a data analyst and in marketing to optimize hotel operations.
  We have extract descriptive analysis:\n
  {descriptive_analysis_data}
"""

# To slow, demand to much memory. But gave good respond
# llm = lms.llm("qwen/qwen3.8-27b")

# MoE model as we do not have enough vram
# llama-server -m Whittle-MoE-27B-A18B-v2.2-Q4_K_M.gguf --host 0.0.0.0 --port 8090 -ngl 99 -c 8192 -fa on --jinja
llm = lms.llm("https://huggingface.co/logic65/Qwen3.8-Whittle-MoE-27B-A17.8B-GGUF")


def clear_console():
    # Windows uses 'cls', macOS/Linux uses 'clear'
    command = "cls" if os.name == "nt" else "clear"

    # shell=True is required because cls and clear are shell built-ins, not standalone executables
    subprocess.run(command, shell=True, check=False)


def load_data() -> pd.DataFrame:
    df = pd.read_csv(DATA_FILE_PATH, sep=";", low_memory=False)
    return df


def descriptive_analysis(df: pd.DataFrame) -> lms.PredictionResult:
    descriptive_analysis_data = df.drop(columns="booking_id").describe().T
    # debug info
    print(AI_PROMT_DESCRIPTIVE_ANALYSIS.format(descriptive_analysis_data=descriptive_analysis_data))
    respond = llm.respond(AI_PROMT_DESCRIPTIVE_ANALYSIS.format(descriptive_analysis_data=descriptive_analysis_data))
    return respond


def ia_conclusion(msg: str) -> str:
    conclusion = f"For NF Hotel we need some conlusion for following: {msg}"
    return conclusion


def run():
    clear_console()
    df = load_data()
    r = descriptive_analysis(df)
    print(r)


# Block direct execution
if __name__ == "__main__":
    print("❌ Error: This script cannot be executed directly.", file=sys.stderr)
    print("Please use command `./run.py`", file=sys.stderr)
    sys.exit(1)
