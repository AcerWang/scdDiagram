import Data_process
import DBHelper

class Drawer(object):
    def __init__(self, trans, buses, lines, line_bus, bus_relation):
        pass
    def draw(self):
        pass
    



if __name__ == '__main__':

    db = DBHelper.DataBase()

    # 获得主变信息
    trans = Data_process.getTransformers(db)
    # 获得母线信息
    buses = Data_process.getBuses(db)
    # 获得线路信息和线路-母线连接关系
    lines, line_bus = Data_process.getLineBus(db)
    # 获得母线连接关系
    bus_relation = Data_process.getBusRelationship(db)

    for i in [trans,buses,lines,line_bus,bus_relation]:
        print(i)

    db.close_connection()

