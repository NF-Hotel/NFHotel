# NFHotel Data Analysis

## Goals

The Python scripts in this project aim to:

1. **Visual presentation of data** using Seaborn and Matplotlib (pyplot)
2. **Data analysis** - perform analysis and use AI to generate conclusions

## Setup

### Create Virtual Environment

```bash
python3 -m venv venv
```

Activate the virtual environment:

**Windows:**
```bash
venv\Scripts\activate
```

**Linux/Mac:**
```bash
source venv/bin/activate
```

### Install Dependencies

```bash
pip3 install -r requiremnts.txt
```

### AI Backend: LM Studio

The Python scripts use [LM Studio](https://lmstudio.ai/) as their AI backend. Instead of calling a cloud LLM API, they talk to LM Studio's local OpenAI-compatible server (default `http://localhost:1234`) via the `lmstudio` Python package.

Before running the analysis, you need to:

1. Install LM Studio and load the model used by the script (`qwen/qwen3.8-27b`)
2. Start the local server (Developer tab → Start Server)

The model is configured in `main.py` (`lms.llm("qwen/qwen3.8-27b")`).
