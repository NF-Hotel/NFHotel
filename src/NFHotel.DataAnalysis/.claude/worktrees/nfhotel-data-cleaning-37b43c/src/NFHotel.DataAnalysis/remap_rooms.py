import pandas as pd

from data_cleaning import clean_data

# Constants
DATAFILE = 'data/hotel_bookings.csv'

def load_data_from_file() -> pd.DataFrame:
  print("Loading data")
  df = pd.read_csv(DATAFILE, sep=';', low_memory=False)
  return df

def data_cleaning(df) -> pd.DataFrame:
  return clean_data(df)

hotel_bookings = load_data_from_file()

print("data:");

print(hotel_bookings.head());
print(hotel_bookings.shape);

hotel_bookings = data_cleaning(hotel_bookings)

# only keep reosrt hotel data
hotel_bookings = hotel_bookings[hotel_bookings['hotel'] != 'City Hotel']

# Map old values to new values
mapping = {
    'Resort Hotel': 'NF Hotel'
}

# Apply changes directly to the column
hotel_bookings['hotel'] = hotel_bookings['hotel'].replace(mapping)

# Show the first 5 and last 5 rows together
print(pd.concat([hotel_bookings.head(), hotel_bookings.tail()]))
print(hotel_bookings.shape)


# # Convert to only twoo room type
# df['assigned_room_type'] = df['assigned_room_type'].replace({'C': 'A', 'D': 'B', 'E': 'A', 'F': 'B','G': 'A', 'H': 'B', 'I': 'A', 'P': 'B', 'L': 'B'})

# print("After remapping:")
# print(df['assigned_room_type'].value_counts())

# # Save the updated CSV
hotel_bookings.to_csv('nf_hotel_bookings.csv', sep=';', index=False)
print("\nFile saved successfully!")
