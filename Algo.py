def union_algo(bus_union):
    '''
        母联分类算法，针对复杂接线形式适用，分离出长短母线
    '''
    tmp_bus = []
    base = []
    for i in bus_union:
        if tmp_bus == []:
            tmp_bus.append(i)
            base.append(i[0])
            continue
        n = 0
        flag = False
        for j in tmp_bus:
            if i[0] in j:
                tmp_bus[n].append(i[1])
                base[n] = i[0]
                flag = True
                break
            if i[1] in j:
                tmp_bus[n].append(i[0])
                base[n] = i[1]
                flag = True
                break
            n += 1
        if not flag:
            tmp_bus.append(i)
            base.append(i[0])
    n = 0
    t = []
    for i in tmp_bus:
        i.remove(base[n])
        i = sorted(i)
        t.append([base[n],]+i)
        n += 1
    return t

# 测试
if __name__ == '__main__':
    
    bus_union = [[1,2],[2,3],[4,5],[4,6]]
    
    bus_list = union_algo(bus_union)
    print(bus_list)