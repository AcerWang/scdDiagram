def union_algo(bus_union):
    '''
        母联分类算法，针对复杂接线形式适用，分离出长短母线，不改变原来的对象
    '''
    tmp_bus = []
    base = []
    for i0,i1 in bus_union:
        if tmp_bus == []:
            tmp_bus.append([i0,i1])
            base.append(i0)
            continue
        n = 0
        flag = False
        for j in tmp_bus:
            if i0 in j:
                tmp_bus[n].append(i1)
                base[n] = i0
                flag = True
                break
            if i1 in j:
                tmp_bus[n].append(i0)
                base[n] = i1
                flag = True
                break
            n += 1
        if not flag:
            tmp_bus.append([i0,i1])
            base.append(i0)
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