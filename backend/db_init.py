import sqlite3

conn = sqlite3.connect("database.db")
cursor = conn.cursor()

cursor.execute("""
CREATE TABLE IF NOT EXISTS post (
    id INTEGER PRIMARY KEY,
    latitude  REAL,
    longitude REAL,
    altitude REAL,
    x REAL,
    y REAL,
    Z REAL,
    W REAL        
)
""")

conn.commit()
conn.close()