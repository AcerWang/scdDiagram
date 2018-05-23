import Data_process
import DBHelper

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
        self.et.write('test.html',encoding='utf-8')
        os.startfile('test.html')

    def draw_bus(self):
        '''
            画母线
        '''
        volt = sorted(self.buses.keys())
        max_volt = volt[-1]
        mid_volt = volt[-2]

        if max_volt == 500 or max_volt == 750:
            # 3/2接线
            x1, x2, y1 = 50, 1050, 200

            for i in [1,2]:
                ele = Element('line')
                ele.attrib = {'id':str(max_volt)+'kV_'+Data_process.char_index[i-1], 'x1':str(x1), 'y1':str(y1), 'x2':str(x2), 'y2':str(y1),'stroke':'red', 'stroke-width':'5'}
                txt = Element('text')
                txt.attrib =  {'dy':"0", 'stroke':"black", 'stroke-width':"0.5", 'x':str(x1-20) ,'y':str(y1+5)}
                txt.text = Data_process.char_index[i-1]
                self.svg.append(ele)
                self.svg.append(txt)
                y1 += 150
                pass
        else:
            # 普通接线，单母线
            if max_volt not in bus_relation:
                ele = Element('line')
                ele.attrib = {'id':str(max_volt)+'kV_I', 'x1':'50', 'y1':'350', 'x2':'1050', 'y2':'350','stroke':'red', 'stroke-width':'5'}
                txt = Element('text')
                txt.attrib =  {'dy':"0", 'stroke':"black", 'stroke-width':"0.5", 'x':'30' ,'y':'355'}
                txt.text = 'I'
                self.svg.append(ele)
                self.svg.append(txt)
                pass
            else:
                if '母联' in bus_relation[max_volt]:
                    if bus_relation[max_volt]['母联'] is None or len(self.buses[max_volt])==2:
                        # 直接并联两条母线
                        x1, x2, y1 = 50, 1050, 350
                        for i in self.buses:
                            ele = Element('line')
                            ele.attrib = {'id':str(max_volt)+'kV_'+Data_process.char_index[i-1], 'x1':str(x1), 'y1':str(y1), 'x2':str(x2), 'y2':str(y1),'stroke':'red', 'stroke-width':'5'}
                            txt = Element('text')
                            txt.attrib =  {'dy':"0", 'stroke':"black", 'stroke-width':"0.5", 'x':str(x1-20) ,'y':str(y1+5)}
                            txt.text = Data_process.char_index[i-1]
                            self.svg.append(ele)
                            self.svg.append(txt)
                            y1 -= 50
                    else:
                        import copy
                        tmp = []
                        bus = copy.copy(self.buses[max_volt])

                        for i in bus_relation[max_volt]['母联']:
                            tmp += i
                        counter = Counter(tmp)
                        most = counter.most_common()
                        for i in most:
                            if i[1]>1:
                                
                        pass
        if mid_volt<100:
            return

    def draw_line(self,line):
        pass

if __name__ == '__main__':


    db = DBHelper.DataBase()
    # 获得主变信息
    trans = Data_process.getTransformers(db)
    # 获得母线信息
    buses = Data_process.getBuses(db)
    # # 获得线路信息和线路-母线连接关系
    # lines, line_bus = Data_process.getLineBus(db)
    # 获得母线连接关系
    bus_relation = Data_process.getBusRelationship(db)
    db.close_connection()


    
    # etree = ET.parse("test.html")
    # drawer = Drawer(et=etree,trans=trans,buses=buses)
    # drawer.draw()
    print(buses)
    print(bus_relation)
