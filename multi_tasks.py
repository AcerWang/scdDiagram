from iter_ele import resolve
from scd_struct import resolve_struct

from threading import Thread
import time


def multi_task(file):
    
    task1 = Thread(target=resolve,args=(file,'IED'))
    task2 = Thread(target=resolve,args=(file,'AccessPoint'))
    task3 = Thread(target=resolve,args=(file,'LDevice'))
    task4 = Thread(target=resolve,args=(file,'DAI','DOI'))
    task5 = Thread(target=resolve_struct,args=(file,))

    task1.start()
    task2.start()
    task3.start()
    task4.start()
    task5.start()

    task1.join()
    task2.join()
    task3.join()
    task4.join()
    task5.join()

    print('done!')

if __name__=='__main__':
    start = time.clock()
    multi_task('HS220.scd')
    end = time.clock()
    print('total time:',end-start,'s')
