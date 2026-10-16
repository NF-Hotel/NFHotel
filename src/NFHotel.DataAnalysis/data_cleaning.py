import pandas as pd

# Columns holding dates stored as 'DD-MM-YYYY' text
DATE_COLUMNS = ["booking_date", "arrival_date"]
DATE_FORMAT = "%d-%m-%Y"

# Columns that carry almost no data and add no analytical value.
# 'company' is ~94% empty (112593/119390 rows) in the source file.
IRRELEVANT_COLUMNS = ["company"]

# Different columns use different literal tokens for "unknown" (e.g. country
# uses '#I/T', meal/market_segment use 'Undefined'). Unify them to one token
# so downstream analysis doesn't have to special-case each column.
PLACEHOLDER_TEXT_VALUES = {
    "country": ["#I/T"],
    "meal": ["Undefined"],
    "market_segment": ["Undefined"],
}

MISSING_TEXT_PLACEHOLDER = "Unknown"

# Numeric columns where a missing value means "none" rather than "unknown"
# (no agent involved, no children on the booking).
ZERO_FILLED_NUMERIC_COLUMNS = ["agent", "children"]


def remove_duplicate_rows(df: pd.DataFrame) -> pd.DataFrame:
    """Drop exact duplicate rows and duplicate booking_id rows, keeping the first occurrence."""
    df = df.drop_duplicates()
    if "booking_id" in df.columns:
        df = df.drop_duplicates(subset="booking_id", keep="first")
    return df.reset_index(drop=True)


def remove_irrelevant_columns(
    df: pd.DataFrame, columns=IRRELEVANT_COLUMNS
) -> pd.DataFrame:
    """Drop columns that carry no analytical value."""
    return df.drop(columns=[c for c in columns if c in df.columns])


def clean_structural_text(df: pd.DataFrame) -> pd.DataFrame:
    """Strip stray whitespace and unify inconsistent 'unknown' placeholder tokens."""
    text_columns = df.select_dtypes(include="object").columns
    for col in text_columns:
        df[col] = df[col].str.strip()
    for col, placeholders in PLACEHOLDER_TEXT_VALUES.items():
        if col in df.columns:
            df[col] = df[col].replace(placeholders, MISSING_TEXT_PLACEHOLDER)
    return df


def fix_data_types(df: pd.DataFrame) -> pd.DataFrame:
    """Convert date columns to datetime and give whole-number columns integer dtypes."""
    for col in DATE_COLUMNS:
        if col in df.columns:
            df[col] = pd.to_datetime(df[col], format=DATE_FORMAT, errors="coerce")
    for col in ZERO_FILLED_NUMERIC_COLUMNS:
        if col in df.columns:
            df[col] = df[col].fillna(0).astype("int64")
    return df


def fill_missing_values(df: pd.DataFrame) -> pd.DataFrame:
    """Replace any remaining missing values with explicit placeholders instead of leaving NaN."""
    text_columns = df.select_dtypes(include="object").columns
    for col in text_columns:
        df[col] = df[col].fillna(MISSING_TEXT_PLACEHOLDER)
    return df


def reset_index(df: pd.DataFrame) -> pd.DataFrame:
    df.reset_index(drop=True, inplace=True)
    return df


def clean_data(df: pd.DataFrame) -> pd.DataFrame:
    """Run the full cleaning pipeline: unique rows, no irrelevant columns,
    consistent text, correct dtypes, and no unmarked missing values."""
    df = remove_duplicate_rows(df)
    df = remove_irrelevant_columns(df)
    df = clean_structural_text(df)
    df = fix_data_types(df)
    df = fill_missing_values(df)
    df = reset_index(df)
    return df
