import sys
from pathlib import Path
from fastapi import FastAPI, Body

# ------------------------------------------------------------------
# Добавляем путь к субмодулю DetectGPT (НЕ МЕНЯЕМ ЕГО СОДЕРЖИМОЕ)
# ------------------------------------------------------------------
BASE_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(BASE_DIR / "DetectGPT"))

from model import GPT2PPLV2 as GPT2PPL

# ------------------------------------------------------------------
# FastAPI
# ------------------------------------------------------------------
app = FastAPI(
    title="DetectGPT API",
    version="1.0"
)

# Модель загружается один раз при старте
model = None

# ------------------------------------------------------------------
# Mapping model result -> AIDetectResult (C#)
# ------------------------------------------------------------------
def map_to_ai_detect_result(model_result):
    """
    model_result:
      (
        {"prob": "87.32%", "label": "human"},
        verdict
      )
    """
    result, _ = model_result

    try:
        # "87.32%" -> 0.8732
        prob_str = result.get("prob", "0").replace("%", "")
        score = float(prob_str) / 100.0

        label = result.get("label", 0)

        if label == 0:
            type_result = 0
        else:
            type_result = 1

        return {
            "score": score,
            "typeResult": type_result,
            "error": None
        }

    except Exception as e:
        return {
            "score": -1,
            "typeResult": 0,
            "error": str(e)
        }

# ------------------------------------------------------------------
# API endpoint
# ------------------------------------------------------------------
@app.post("/detect")
def detect(text: str = Body(..., embed=True)):
    global model
    if model is None:
        from DetectGPT.model import GPT2PPLV2 as GPT2PPL
        model = GPT2PPL()
    try:
        model_result = model(text, 100, "v1.1")
        return map_to_ai_detect_result(model_result)

    except Exception as e:
        return {
            "score": -1,
            "typeResult": 0,
            "error": str(e)
        }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("server:app", host="0.0.0.0", port=5555, reload=True)