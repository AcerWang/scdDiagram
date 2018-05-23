import Data_process
import DBHelper
import Algo

from collections import Counter
import xml.etree.ElementTree as ET
from xml.etree.ElementTree import Element
import os

class Drawer(object):
    
    def __init__(self, et=None, trans=None, buses=None, lines=None, line_bus=None, bus_relation=None):
        self.et = et
        self.trans = trans
        self.buses = buses
        self.lines = lines
        self.line_bus = line_bus
        self.bus_relation = bus_relation
        self.svg = et.getroot().find('body/svg')

    def draw_transfomer(self):
        '''
            画变压器
        '''
        if self.trans is None:
            return
        x, y = 0, 0
        for t in self.trans:
            ele_t = Element('use')
            x += 400
            y = 450
            ele_t.attrib = {'id': 'T'+str(t), 'x':str(x), 'y':str(y) , 'href':'#Trans'}
            
            txt = Element('text')
            txt.attrib =  {'dy':"0", 'stroke':"black", 'stroke-width':"0.5", 'x':str(x-20) ,'y':str(y+25)}
            txt.text = "T"+str(t)

            self.svg.append(ele_t)
            self.svg.append(txt)

    def draw(self):
        '''
            输出成html文件，并显示
        '''
        self.draw_transfomer()
        self.draw_bus()
        self.et.write('index.html',encoding='utf-8')
        os.startfile('index.html')

    def draw_bus(self):
        '''
            画母线
        '''
        volt = sorted(self.buses.keys())
        max_volt = volt[-1]
        mid_volt = volt[-2]

        # 高压侧母线
        if max_volt == 500 or max_volt == 750:
            # 3/2接线
            x1, x2, y1 = 50, 1050, 200
            for i in [1,2]:
                self.singleBus(max_volt,i,x1=x1,x2=x2,y=y1)
                y1 += 150
        else:
            # 普通接线，单母线
            if max_volt not in bus_relation:
                self.singleBus(max_volt)
            else:
                if '母联' in bus_relation[max_volt]:
                    # 直接并联两条母线
                    if bus_relation[max_volt]['母联'] is None or len(self.buses[max_volt])==2:
                        x1, x2, y1 = 50, 1050, 350
                        for i in self.buses[max_volt]:
                            self.singleBus(max_volt,i,x1=x1,x2=x2,y=y1)
                            y1 -= 50
                    
                    # 多组母联
                    else:
                        bus_list = Algo.union_algo(bus_relation[max_volt]['母联'])
                        x1, x2, y1 = 50, 1050, 350
                        # 分组数
                        num_sep = len(bus_list)
                        # 2分组，每组长度600
                        if num_sep==2:
                            x2 = 650
                        # 3分组，每组长度400
                        if num_sep==3:
                            x2 = 450
                        # 下面的单独一段母线长度
                        singleLen = x2-x1

                        for bus in bus_list:
                            # 母联下面的一条母线
                            self.singleBus(max_volt,bus[0],x1=x1,x2=x2,y=y1)

                            # 母联上面的母线
                            length = len(bus[1:])
                            partLen = (singleLen-(length-1)*50)//length
                            x2 = x1 + partLen
                            y1 = y1 - 40
                            for i in bus[1:]:
                                self.singleBus(max_volt,i,x1=x1,x2=x2,y=y1)
                                x1 = x2 + 50
                                x2 = x1 + partLen
                            x1 = x2 - x1 + 50
                            x2 = x1 + singleLen
                            y1 = y1 + 40
                                
        # 中压侧母线
        if mid_volt<100:
            return
        # 普通接线，单母线
        if mid_volt not in bus_relation:
            self.singleBus(mid_volt,)

    def singleBus(self, volt, bus_no=1, x1=50, x2=1050, y=350, color='red'):
        '''
            画单一母线
        '''
        ele = Element('line')
        ele.attrib = {'id':str(volt)+'kV_'+Data_process.char_index[bus_no-1], 'x1':str(x1), 'y1':y, 'x2':str(x2), 'y2':y,'stroke':color, 'stroke-width':'5'}
        txt = Element('text')
        txt.attrib =  {'dy':"0", 'stroke':"black", 'stroke-width':"0.5", 'x':str(x1-20) ,'y':str(y+5)}
        txt.text = Data_process.char_index[bus_no-1]
        self.svg.append(ele)
        self.svg.append(txt)

    def draw_line(self,line):
        pass

if __name__ == '__main__':


    db = DBHelper.DataBase()
    # 获得主变信息
    trans = Data_process.getTransformers(db)
    # 获得母线信息
    buses = Data_process.getBuses(db)
    # 获得[线路信息]和[线路-母线]连接关系
    lines, line_bus = Data_process.getLineBus(db)
    # 获得母线连接关系
    bus_relation = Data_process.getBusRelationship(db)
    db.close_connection()


    
    etree = ET.parse("base.html")
    drawer = Drawer(et=etree,trans=trans,buses=buses)
    drawer.draw()
    print('Down.')
    # print(buses)
    # print(bus_relation)
