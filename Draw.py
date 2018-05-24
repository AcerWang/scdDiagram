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
        self.high_bus = {}
        self.mid_bus = {}
        self.svg = et.getroot().find('body/svg')

    def draw_transfomer(self):
        '''
            画变压器
        '''
        if self.trans is None:
            return
        x, y = 0, 450
        for t in self.trans:
            ele_t = Element('use')
            x += 400
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
        self.draw_bus_union()
        self.draw_bus_seg()
        self.et.write('index.html',encoding='utf-8')
        os.startfile('index.html')

    def draw_bus_union(self):
        '''
            画母联母线
        '''
        volt = sorted(self.buses.keys())
        max_volt = volt[-1]
        mid_volt = volt[-2]
        
        # 存储需要的电压等级，以便后面其他地方使用
        self.high_volt = max_volt
        self.mid_volt = mid_volt

        # 高压侧母线
        if max_volt == 500 or max_volt == 750:
            # 3/2接线
            x1, x2, y1 = 50, 1050, 200
            self.high_bus = {}
            for i in [1,2]:
                self.singleBus(max_volt,i,x1=x1,x2=x2,y=y1)
                self.high_bus[i] = [x1,x2,y1]
                y1 += 150
        else:
            # 普通接线，不存在母联和分段的情况，母线单独放置对应每台变压器
            if max_volt not in self.bus_relation:
                x1, x2 = 50, 1050
                seg = 0
                n = len(self.buses[max_volt])
                if n == 2:
                    x2 = 650
                    seg = 600
                if n == 3:
                    x2 = 450
                    seg = 400
                # 用于保存画线母线
                self.high_bus = {}
                for i in self.buses[max_volt]:
                    self.singleBus(max_volt,bus_no=i,x1=x1,x2=x2,y=600)
                    self.high_bus[i] = [x1,x2,600]
                    x1 = x1 + seg + 50
                    x2 = x1 + seg
                    # 添加已画母线列表
            # 有母线连接关系    
            else:
                if '母联' in self.bus_relation[max_volt]:
                    # 直接并联两条母线
                    if self.bus_relation[max_volt]['母联'] is None:
                        self.high_bus = {}
                        x1, x2, y1 = 50, 1050, 350
                        for i in self.buses[max_volt]:
                            self.singleBus(max_volt,i,x1=x1,x2=x2,y=y1)
                            self.high_bus[i] = [x1,x2,y1]
                            y1 -= 40
                            # 添加到已输出的列表中

                        self.busUnion(x=x1+20,y=y1+40)
                    # 多组母联
                    else:
                        bus_list = Algo.union_algo(self.bus_relation[max_volt]['母联'])
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
                        
                        self.high_bus = {}
                        for bus in bus_list:
                            # 母联下面的一条母线
                            self.singleBus(max_volt,bus[0],x1=x1,x2=x2,y=y1)
                            
                            # 添加到已输出的列表中
                            self.high_bus[bus[0]] = [x1,x2,y1]
                            # 母联上面的母线
                            length = len(bus[1:])
                            partLen = (singleLen-(length-1)*50)//length
                            x2 = x1 + partLen
                            y1 = y1 - 40
                            for i in bus[1:]:
                                self.singleBus(max_volt,i,x1=x1,x2=x2,y=y1)
                                # 添加到已输出的列表中
                                self.high_bus[i] = [x1,x2,y1]
                                x1 = x2 + 50
                                x2 = x1 + partLen
                            x1 = x2 - x1 + 100
                            x2 = x1 + singleLen
                            y1 = y1 + 40
                                
        # 中压侧母线
        if mid_volt<100:
            return

        # 普通接线，不存在母联和分段的情况，母线单独放置对应每台变压器
        if mid_volt not in self.bus_relation:
            x1, x2 = 50, 1050
            seg = 0
            n = len(self.buses[mid_volt])
            if n == 2:
                x2 = 650
                seg = 600
            if n == 3:
                x2 = 450
                seg = 400
            # 用于保存已经画线母线
            self.mid_bus = {}
            for i in self.buses[mid_volt]:
                self.singleBus(mid_volt,bus_no=i,x1=x1,x2=x2,y=600)
                # 添加已画母线列表
                self.mid_bus[i] = [x1,x2,350]
                x1 = x1 + seg + 50
                x2 = x1 + seg
            
        else:
            if '母联' in self.bus_relation[mid_volt]:
                # 直接并联两条母线
                if self.bus_relation[mid_volt]['母联'] is None:
                    # 用于保存已画线母线
                    self.mid_bus = {}
                    x1, x2, y1 = 50, 1050, 600
                    for i in self.buses[mid_volt]:
                        self.singleBus(mid_volt,i,x1=x1,x2=x2,y=y1,color='blue')
                        # 添加到已输出的列表中
                        self.mid_bus[i] = [x1,x2,y1]
                        y1 += 40
                    self.busUnion(x=x1+20,y=y1,color='blue',href='#BusUnion-down')
                # 多组母联
                else:
                    bus_list = Algo.union_algo(self.bus_relation[mid_volt]['母联'])
                    x1, x2, y1 = 50, 1050, 600
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
                    # 保存已输出的母线段
                    self.mid_bus = {}
                    for bus in bus_list:
                        # 母联上面的一条母线
                        self.singleBus(mid_volt,bus[0],x1=x1,x2=x2,y=y1,color='blue')
                        # 保存到已输出的列表
                        self.mid_bus[bus[0]] = [x1,x2,y1]
                        # 母联下面的母线
                        length = len(bus[1:])
                        partLen = (singleLen-(length-1)*50)//length
                        x2 = x1 + partLen
                        y1 = y1 + 40
                        for i in bus[1:]:
                            self.singleBus(mid_volt,i,x1=x1,x2=x2,y=y1,color='blue')
                            self.busUnion(x=x1+10,y=y1-70,color='blue',href='#BusUnion-down')
                            # 保存到已输出的列表
                            self.mid_bus[i] = [x1,x2,y1]
                            x1 = x2 + 50
                            x2 = x1 + partLen
                        x1 = x2 - x1 + 100
                        x2 = x1 + singleLen
                        y1 = y1 - 40

    def draw_bus_seg(self):
        '''
            画分段开关及母线
        '''
        # 分段开关存在高压侧
        if self.high_volt in self.bus_relation and '分段' in self.bus_relation[self.high_volt]:
            # 默认二分段
            if self.bus_relation[self.high_volt]['分段'] is None:
                self.high_bus = {}
                self.singleBus(self.high_volt,bus_no=1,x1=50,x2=650,y=350)
                self.high_bus[1] = [50,650,350]
                self.singleBus(self.high_volt,bus_no=2,x1= 700,x2=1300,y=350)
                self.high_bus[1] = [700,1300,350]
                self.busSeg(x=640,y=330)
            # 有多分段的情况
            else:
                # 分段组数
                seg_len = len(self.bus_relation[self.high_volt]['分段'])
                segs_total = []
                for seg in self.bus_relation[self.high_volt]['分段']:
                    segs_total += seg
                segs_total = len(set(segs_total))
                # 分组数比分段数少2，并联型分段
                if segs_total-seg_len == 2:
                    pass
                # 串联型分段
                x1,x2,part_len,y = 50,1050,1000,350
                if segs_total-seg_len == 1:
                    part_len = 1000
                    for i in range(1,segs_total):
                        part_len -= 400//i 
                    x2 = x1+part_len

                for seg in self.bus_relation[self.high_volt]['分段']:
                    seg = sorted(seg)
                    # 两段已经存在，直接画分段开关
                    if seg[0] in self.high_bus and seg[1] in self.high_bus:
                        x2 = self.high_bus[seg[0]][1]
                        y1 = self.high_bus[seg[0]][2]
                        self.busSeg(x=x2-25,y=y1-25)
                        continue
                    # 如果第二段不存在,画出母线段，再画分段
                    if seg[0] in self.high_bus and seg[1] not in self.high_bus:
                        x1 = self.high_bus[seg[0]][0]
                        x2 = self.high_bus[seg[0]][1]
                        y1 = self.high_bus[seg[1]][2]
                        self.singleBus(volt=self.high_volt,bus_no=seg[1],x1=x2+50,x2=2*x2-x1+50,y=y1)
                        self.high_bus[seg[1]] = [x2+50,2*x2-x1+50,y1]
                        self.busSeg(x=x2-25,y=y1-25)
                        continue
                    # 如果两段都不存在，画出两段再画分段开关
                    if seg[0] not in self.high_bus and seg[1] not in self.high_bus:
                        for i in seg:
                            self.singleBus(volt=self.high_volt,bus_no=i,x1=x1,x2=x2,y=y1)
                            self.high_bus[i] = [x1,x2,y1]
                            x1 = x2 + 50
                            x2 = x1 + part_len
                        self.busSeg(self.high_bus[seg[0]][1]-25,y1-25)
                        continue

        # 分段开关存在中压侧
        if self.mid_volt in self.bus_relation and '分段' in self.bus_relation[self.mid_volt] and self.mid_volt>100:
            # 默认二分段
            if self.bus_relation[self.mid_volt]['分段'] is None:
                self.high_bus = {}
                self.singleBus(self.mid_volt,bus_no=1,x1=50,x2=650,y=600)
                self.high_bus[1] = [50,650,600]
                self.singleBus(self.high_volt,bus_no=2,x1= 700,x2=1300,y=600)
                self.high_bus[1] = [700,1300,600]
                self.busSeg(x=640,y=580)
            # 有多分段的情况
            else:
                # 分段组数
                seg_len = len(bus_relation[self.mid_volt]['分段'])
                segs_total = []
                for seg in self.bus_relation[self.mid_volt]['分段']:
                    segs_total += seg
                segs_total = len(set(segs_total))
                # 分组数比分段数少2，并联型分段
                if segs_total-seg_len == 2:
                    pass
                # 串联型分段
                x1,x2,part_len,y1 = 50,1050,1000,600
                if segs_total-seg_len == 1:
                    part_len = 1000
                    for i in range(1,segs_total):
                        part_len -= 400//i 
                    x2 = x1+part_len

                for seg in self.bus_relation[self.mid_volt]['分段']:
                    seg = sorted(seg)
                    # 两段已经存在，直接画分段开关
                    if seg[0] in self.mid_bus and seg[1] in self.mid_bus:
                        x = self.mid_bus[seg[0]][1]
                        y = self.mid_bus[seg[0]][2]
                        self.busSeg(x=x-25,y=y-25,color='blue')
                        continue
                    # 如果第二段不存在,画出母线段，再画分段
                    if seg[0] in self.mid_bus and seg[1] not in self.mid_bus:
                        x1 = self.mid_bus[seg[0]][0]
                        x2 = self.mid_bus[seg[0]][1]
                        y1 = self.mid_bus[seg[0]][2]
                        self.singleBus(volt=self.mid_volt,bus_no=seg[1],x1=x2+50,x2=2*x2-x1+50,y=y1,color='blue')
                        self.mid_bus[seg[1]] = [x2+50,2*x2-x1+50,y1]
                        self.busSeg(x=x2-25,y=y1-25,color='blue')
                        continue
                    # 如果两段都不存在，画出两段再画分段开关
                    if seg[0] not in self.mid_bus and seg[1] not in self.mid_bus:
                        for i in seg:
                            self.singleBus(volt=self.mid_volt,bus_no=i,x1=x1,x2=x2,y=y1,color='blue')
                            self.mid_bus[i] = [x1,x2,y1]
                            x1 = x2 + 50
                            x2 = x1 + part_len
                        self.busSeg(self.mid_bus[seg[0]][1]-25,y1-25,color='blue')
                        continue

    def singleBus(self, volt, bus_no=1, x1=50, x2=1050, y=350, color='red'):
        '''
            画单一母线
        '''
        ele = Element('line')
        ele.attrib = {'id':str(volt)+'kV_'+Data_process.char_index[bus_no-1], 'x1':str(x1), 'y1':str(y), 'x2':str(x2), 'y2':str(y),'stroke':color, 'stroke-width':'5'}
        txt = Element('text')
        txt.attrib =  {'dy':"0", 'stroke':"black", 'stroke-width':"0.5", 'x':str(x1-20) ,'y':str(y+5)}
        txt.text = Data_process.char_index[bus_no-1]
        self.svg.append(ele)
        self.svg.append(txt)

    def busUnion(self,x,y,color='red',href='#BusUnion'):
        '''
            母联开关
        '''
        ele = Element('use')
        ele.attrib = {'x':str(x), 'y':str(y), 'stroke':color, 'href':href}
        self.svg.append(ele)
        pass

    def busSeg(self,x,y,color='red',href='#BusSeg'):
        '''
            分段开关
        '''
        ele = Element('use')
        ele.attrib = {'x':str(x), 'y':str(y), 'stroke':color, 'href':href}
        self.svg.append(ele)
        pass

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

    print(buses)
    print(bus_relation)
    
    etree = ET.parse("base.html")
    drawer = Drawer(et=etree,trans=trans,buses=buses,bus_relation=bus_relation)
    drawer.draw()
    print('Down.')
    # print(buses)
    # print(bus_relation)
