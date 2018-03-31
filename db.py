import sqlite3
'''
   本模块负责处理数据库操作，
   为数据连接层
'''

conn = None
cursor = None

def get_db():
    """"
       如果连接存在，直接返回该连接
       如果连接不存在，创建一个新的连接，并返回该连接
    """
    global conn
    
    if conn:
        return conn
    
    try:
        conn = sqlite3.connect("SCD.db")
    except Exception as e:
        print(e)
    return conn

def close_db():
    """"
       如果连接存在，关闭数据库连接
    """
    global conn
    if conn:
        conn.close()

def get_cursor():
    """
       获取游标对象
    """
    global cursor
    if cursor:
        return cursor
    cursor = conn.cursor()
    return cursor

def close_cursor():
    """
       关闭游标对象
    """
    global cursor
    if cursor:
        cursor.close()

def insert(sql_insert):
    """
       插入数据到数据库中
    """
    global cursor
    cursor.execute(sql_insert)
    print(cursor.rowcount)

def init():
    """
       初始化数据库连接对象和游标对象
    """
    global conn,cursor
    conn = get_db()
    cursor = get_cursor()

def final():
    """
       关闭游标对象和数据库连接对象
    """
    close_cursor()
    close_db()
