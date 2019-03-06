import xml.etree.ElementTree as ET
from xml.etree.ElementTree import Element
import os
from collections import defaultdict

import Data_process
import DBHelper
import Algo


class Drawer(object):
    
    def __init__(self, et=None, trans=None, buses=None, lines=None, line_bus=None, bus_relation=None):
        self.et = et
        self.trans = trans
        self.buses = buses
        self.lines = lines
        self.line_bus = line_bus
        self.bus_relation = bus_relation
        
        # 用于记录已画线的母线段
        self.high_bus = {}
        self.mid_bus = {}

        # 用于记录已画线的线路-母线
        self.bus_line_high = defaultdict(int)
        self.bus_line_mid = defaultdict(int)

        # 用于记录高压3/2断路器数的信息(除主变间隔对应的外)
        self.num_high_breaker = 0
        # 用于记录断路器位置信息，初始位置横坐标x=20
        self.high_breaker = [20,]
        
        # 记录变压器的信息
        self.t = {}

        # 获取模板文件的svg节点
        self.svg = et.getroot().find('body/svg')

    def draw(self):
        '''
            输出成html文件，并显示
        '''
        self.draw_transfomer()
        self.draw_bus_union()
        self.draw_bus_seg()
        self.draw_line()
        self.draw_join()
        self.et.write('index.html',encoding='utf-8')
        os.startfile('index.html')
    
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
            # 记录变压器信息
            self.t[t] = [x,y]

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
            x1, x2, y1 = 50, 1250, 200
            self.high_bus = {}
            for i in [1,2]:
                self.singleBus(max_volt,i,x1=x1,x2=x2,y=y1)
                self.high_bus[i] = [x1,x2,y1]
                y1 += 150
        else:
            # 普通接线，不存在母联和分段的情况，母线单独放置对应每台变压器
            if max_volt not in self.bus_relation:
                x1, x2 = 50, 1250
                seg = 0
                n = len(self.buses[max_volt])
                if n == 2:
                    x2 = 650
                    seg = 600
                if n == 3:
                    x2 = 450
                    seg = 400
                
                for i in self.buses[max_volt]:
                    self.singleBus(max_volt,bus_no=i,x1=x1,x2=x2,y=350)
                    # 添加已画母线列表
                    self.high_bus[i] = [x1,x2,350]
                    x1 = x1 + seg + 50
                    x2 = x1 + seg
            # 有母线连接关系    
            else:
                if '母联' in self.bus_relation[max_volt]:
                    # 直接并联两条母线
                    if self.bus_relation[max_volt]['母联'] is None:
                        if len(self.buses[max_volt])==1:
                            self.buses[max_volt].append(self.buses[max_volt][0]+1)
                        x1, x2, y1 = 50, 1250, 350
                        for i in self.buses[max_volt]:
                            self.singleBus(max_volt,i,x1=x1,x2=x2,y=y1)
                            self.high_bus[i] = [x1,x2,y1]
                            y1 -= 40
                            # 添加到已输出的列表中

                        self.busUnion(x=x1+20,y=y1+40)
                    # 多组母联
                    else:
                        bus_list = Algo.union_algo(self.bus_relation[max_volt]['母联'])
                        x1, x2, y1 = 50, 1250, 350
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
            x1, x2 = 50, 1250
            seg = 0
            n = len(self.buses[mid_volt])
            if n == 2:
                x2 = 650
                seg = 600
            if n == 3:
                x2 = 450
                seg = 400
            
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
                    if len(self.buses[mid_volt])==1:
                            self.buses[mid_volt].append(self.buses[mid_volt][0]+1)
                    x1, x2, y1 = 50, 1250, 600
                    for i in self.buses[mid_volt]:
                        self.singleBus(mid_volt,i,x1=x1,x2=x2,y=y1,color='blue')
                        # 添加到已输出的列表中
                        self.mid_bus[i] = [x1,x2,y1]
                        y1 += 40
                    self.busUnion(x=x1+20,y=570,color='blue',href='#BusUnion-down')
                # 多组母联
                else:
                    bus_list = Algo.union_algo(self.bus_relation[mid_volt]['母联'][:])
                    x1, x2, y1 = 50, 1250, 600
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
                x1,x2,part_len,y = 50,1250,1200,350
                if segs_total-seg_len == 1:
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
                x1,x2,part_len,y1 = 50,1250,1200,600
                if segs_total-seg_len == 1:
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

    def draw_line(self):
        '''
            画线路
        '''
        # 特殊情况，需要单独考虑
        if self.line_bus =={}:
            if self.high_volt>=500:
                pass
            else:
                
                i = 1 if len(self.high_bus)==1 else 2
                for line in self.lines:
                    if int(line[:2]+'0')==self.high_volt:
                        name = self.lines[line]
                        x = self.high_bus[i][0] + 40*(self.bus_line_high[i]+1)
                        y = self.high_bus[i][2]-190
                        self.singleLine(line=line,name=name,x=x,y=y,href='#Line-Up-'+str(i))
                        self.bus_line_high[i] += 1
            if self.mid_volt<=100:
                pass
            else:
                i = 1 if len(self.mid_bus)==1 else 2
                for line in self.lines:
                    if int(line[:2]+'0')==self.mid_volt and len(line)==4:
                        name = self.lines[line]
                        x = self.mid_bus[i][0] + 40*(self.bus_line_mid[i]+1)
                        y = self.mid_bus[i][2]-40
                        self.singleLine(line=line,name=name,x=x,y=y,href='#Line-Down-'+str(i),color='blue')
                        self.bus_line_mid[i] += 1
            
        # 有母线-线路关系
        lines = sorted(self.line_bus)
        one_half_trans = []
        for line in lines:
            # 3/2 接线
            if int(line[:2]+'0')>=500:
                # 对应变压器的线路
                if int(line[-2:]) in self.t:
                    one_half_trans.append(int(line[-1]))
                    x = self.t[int(line[-1])][0] - 15
                    y = self.high_bus[1][2]
                    self.one_and_half_breaker(x=x,y=y)
                    self.singleLine(line=line,name=self.lines[line],x=x-10,y=y-150,href='#Line-3/2-L')
                    self.singleLine(line="To_"+line[-1],x=x+15,y=y+95,href="#Line-to-Trans")
                
                # 其他线路，画线和画断路器
                else:
                    if self.num_high_breaker%2 == 0:
                        last_x = self.high_breaker[-1]
                        x = last_x + 60

                        # 比较断路器位置到主变对应断路器位置的距离
                        for t in self.trans:
                            if self.t[t][1]-x<40:
                                x = self.t[t][1]+40
                                break
                        y = self.high_bus[1][2]
                        self.one_and_half_breaker(x=x,y=y)
                        # 把对应的断路器横坐标记录下来
                        self.high_breaker.append(x)
                        self.singleLine(line=line,name=self.lines[line],x=x-10,y=y-150,href='#Line-3/2-L')
                    else:
                        x = self.high_breaker[-1]
                        y = self.high_bus[1][2]
                        self.singleLine(line=line,name=self.lines[line],x=x+10,y=y-150,href='#Line-3/2-R')
                    self.num_high_breaker += 1

                continue
            
            # 除3/2以外的普通高压侧的接线方式
            if int(line[:2]+'0')==self.high_volt:
                name = self.lines[line]
                buses = sorted(self.line_bus[line])
                bus_num = len(buses)
                x,y = 0,0

                # 线路只在一条母线上
                if bus_num == 1:
                    x = self.high_bus[buses[0]][0] + 40*(self.bus_line_high[buses[0]]+1)
                    y = self.high_bus[buses[0]][2]-190
                    self.singleLine(line=line,name=name,x=x,y=y,href='#Line-Up-1')
                    self.bus_line_high[buses[0]] += 1
                
                # 线路在两条母线上
                else:
                    x = self.high_bus[buses[0]][0] + 40*(self.bus_line_high[buses[0]]+1)
                    y = self.high_bus[buses[0]][2]-230
                    self.singleLine(line=line,name=name,x=x,y=y,href='#Line-Up-2')
                    self.bus_line_high[buses[0]] += 1
                continue
            
            # 普通的中压侧的接线方式
            if int(line[:2]+'0')==self.mid_volt:
                name = self.lines[line]
                buses = sorted(self.line_bus[line])
                bus_num = len(buses)
                x,y = 0,0
                # 线路只在一条母线上
                if bus_num == 1:
                    x = self.mid_bus[buses[0]][0] + 40*(self.bus_line_mid[buses[0]]+1)
                    y = self.mid_bus[buses[0]][2]-40
                    self.singleLine(line=line,name=name,color='blue',x=x,y=y,href='#Line-Down-1')
                    self.bus_line_mid[buses[0]] += 1

                # 线路在两条母线上
                else:
                    x = self.mid_bus[buses[0]][0] + 40*(self.bus_line_mid[buses[0]]+1)
                    y = self.mid_bus[buses[0]][2]
                    self.singleLine(line=line,name=name,color='blue',x=x,y=y,href='#Line-Down-2')
                    self.bus_line_mid[buses[0]] += 1
                continue
        one_half_trans = sorted(one_half_trans)
        if self.high_volt>=500 and one_half_trans == []:
            for i in self.t:
                x = self.t[i][0] - 15
                y = self.high_bus[1][2]
                self.one_and_half_breaker(x=x,y=y)
                self.singleLine(line="To_"+line[-1],x=x+15,y=y+95,href="#Line-to-Trans")

    def draw_join(self):
        '''
            画主变到母线的连接线
        '''
        # 高压侧
        if self.high_volt>=500:
            pass
        else:
            if self.high_volt in self.bus_relation and '母联' in self.bus_relation[self.high_volt]:
                # 简单的两条并联母线，各台主变直接全接到这两条线上
                if self.bus_relation[self.high_volt]['母联'] is None or len(self.bus_relation[self.high_volt]['母联'])==1:
                    y= self.high_bus[1][2] - 42
                    for t in self.t:
                        x = self.t[t][0]
                        self.joinTrans(x=x,y=y,href='#Join-U-2')
                
                # 多组母联的情况
                else:
                    i = 0
                    for bus in self.bus_relation[self.high_volt]['母联']:
                        x1 = self.high_bus[bus[1]][0]
                        x2 = self.high_bus[bus[1]][1]
                        x = (x1+x2)//2
                        y = self.high_bus[bus[1]][2]
                        self.joinTrans(x=x,y=y,href='#Join-U-2')
                        t_x = self.t[self.trans[i]][0] + 20
                        t_y = self.t[self.trans[i]][1] + 35
                        self.path([str(t_x),str(t_y)],[str(x+20),str(y)],color='red')
                        i += 1
            else:
                # 只有一段或多段母线
                if len(self.buses[self.high_volt])==1:
                    y = self.high_bus[self.buses[self.high_volt][0]][2]
                    for t in self.t:
                        x = self.t[t][0]
                        self.joinTrans(x=x,y=y,href='#Join-U-1')
                # 每段母线接到对应的主变上
                else:
                    for t in self.trans:
                        x1 = self.high_bus[t][0]
                        x2 = self.high_bus[t][1]
                        y = self.high_bus[t][2]
                        x = (x2+x1)//2
                        self.joinTrans(x=x,y=y,href='#Join-U-1')
                        t_x = self.t[t][0] + 20
                        t_y = self.t[t][1]
                        self.path([str(t_x),str(t_y)],[str(x+20),str(y)],color='red')

        # 中压侧
        if self.mid_volt<100:
            pass
        else:
            if '母联' in self.bus_relation[self.mid_volt]:
                # 简单的两条并联母线，各台主变直接全接到这两条线上
                if self.bus_relation[self.mid_volt]['母联'] is None or len(self.bus_relation[self.mid_volt]['母联'])==1:
                    y= self.mid_bus[1][2] - 110
                    for t in self.t:
                        x = self.t[t][0]
                        
                        self.joinTrans(x=x,y=y,href='#Join-D-2')
                # 多组母联的情况
                else:
                    i = 0
                    for bus in self.bus_relation[self.mid_volt]['母联']:
                        x1 = self.mid_bus[bus[1]][0]
                        x2 = self.mid_bus[bus[1]][1]
                        x = (x1+x2)//2
                        y = self.mid_bus[bus[1]][2] - 150
                        self.joinTrans(x=x,y=y,href='#Join-D-2')
                        # 连接主变到母线连接线的线段
                        t_x = self.t[self.trans[i]][0] + 20
                        t_y = self.t[self.trans[i]][1] + 35
                        self.path([str(t_x),str(t_y)],[str(x+20),str(y)],color='blue')
                        i += 1

            else:
            # 只有一段母线
                if len(self.buses[self.mid_volt])==1:
                    y = self.mid_bus[self.buses[self.mid_volt][0]][2]
                    for t in self.t:
                        x = self.t[t][0] + 35
                        self.joinTrans(x=x,y=y,href='#Join-D-1')
            # 每段母线接到对应的主变上
                else:
                    for t in self.trans:
                        x1 = self.mid_bus[t][0]
                        x2 = self.mid_bus[t][1]
                        y = self.mid_bus[t][2] - 110
                        x = (x2+x1)//2
                        self.joinTrans(x=x,y=y,href='#Join-D-1')
                        t_x = self.t[t][0] + 20
                        t_y = self.t[t][1] + 35
                        self.path([str(t_x),str(t_y)],[str(x+20),str(y)],color='blue')

    def one_and_half_breaker(self,x=100,y=200):
        '''
            画3/2接线
        '''
        ele = Element('use')
        ele.attrib = {'x':str(x), 'y':str(y),'href':'#Breaker-3'}
        self.svg.append(ele)

    def path(self,start_P,end_P,color='red'):
        '''
            画路径
        '''
        p = 'M'+' '.join(start_P)
        if int(start_P[1])<int(end_P[1]):
            start_P[1] = str(int(start_P[1])+5)
        p += "L"+' '.join(start_P)
        p += "L"+' '.join(end_P)
        ele = Element('path')
        ele.attrib = {'d':p,'fill-opacity':'0.0','stroke':color}
        self.svg.append(ele)

    def joinTrans(self,x=0,y=0,href="#"):
        '''
            画主变到母线的连接线
        '''
        ele = Element('use')
        ele.attrib = {'x':str(x), 'y':str(y), 'href':href}
        self.svg.append(ele)

    def singleLine(self,line='1',name=None,x=0,y=0,color='red',href='#Line-Up-1'):
        '''
            画单一线路
        '''
        ele = Element('use')
        ele.attrib = {'id':line, 'x':str(x), 'y':str(y), 'stroke':color, 'href':href}
        if name is not None:
            txt = Element('text')
            if 'Up' in href:
                txt.attrib =  {'dy':"0", 'stroke':"black", 'stroke-width':"0.3", 'style':'writing-mode:tb;' , 'x':str(x+5) ,'y':str(y)}
            elif 'Down' in href:
                txt.attrib =  {'dy':"0", 'stroke':"black", 'stroke-width':"0.3", 'style':'writing-mode:tb;' , 'x':str(x+5) ,'y':str(y+140)}
            elif '#Line-3/2-R'==href:
                txt.attrib =  {'dy':"0", 'stroke':"black", 'stroke-width':"0.3", 'style':'writing-mode:tb;' , 'x':str(x+15) ,'y':str(y)}
            else:
                txt.attrib =  {'dy':"0", 'stroke':"black", 'stroke-width':"0.3", 'style':'writing-mode:tb;' , 'x':str(x-5) ,'y':str(y)}
            txt.text = name
            self.svg.append(txt)
        self.svg.append(ele)
    
    def singleBus(self, volt, bus_no=1, x1=50, x2=1250, y=350, color='red'):
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

if __name__ == '__main__':


    db = DBHelper.DataBase()
    # 获得主变信息
    trans = Data_process.getTransformers(db)
    # 获得母线连接关系
    bus_relation = Data_process.getBusRelationship(db)
    # 获得母线信息
    buses = Data_process.getBuses(db)
    # 获得[线路信息]和[线路-母线]连接关系
    lines, line_bus = Data_process.getLineBus(db,bus_relation)
    db.close_connection()


    # print(trans)
    print(bus_relation)
    print(lines)
    print(line_bus)
    print(buses)
    
    etree = ET.parse("base.html")
    drawer = Drawer(et=etree,trans=trans,buses=buses,lines=lines,line_bus=line_bus,bus_relation=bus_relation)
    drawer.draw()
    print('Done.')
