import sqlite3

def getTags(root, *attrs):
    '''
        获取标签tag下的一组属性值，包装成tuple类型，添加到列表里返回
    '''
    lst = []
    count = 0
    for item in root:
        count += 1
        ls = []
        for attr in attrs:
            ls.append(item.get(attr))
        lst.append(tuple(ls))
    print('总共%d条数据'%count)
    return lst

'''
insert into ied values(?,?,?,?,?,?)
'''
def writeDB(sql, data):
    '''
        将之前获得的标签属性值，写入到数据库中
    '''
    try:
        conn = sqlite3.connect('HS220.db')
        cursor = conn.cursor()
        
        cursor.executemany(sql,data)
        conn.commit()
        cursor.close()
        conn.close()
    except Exception as e:
	    print(e)

'''
DROP TABLE IF EXISTS ied; 
CREATE TABLE ied(
						id int auto_increment primary key,
						name varchar(40), 
						desc varchar(80),
						type varchar(20), 
						manufacturer varchar(40),
						configVersion varchar(20))
'''
def createTable(sql):
    '''
        创建表的语句
    '''
    try:
        conn = sqlite3.connect('HS220.db')
        cursor = conn.cursor()
        
        cursor.execute(sql)
        conn.commit()
        cursor.close()
        conn.close()
    except Exception as e:
	    print(e)