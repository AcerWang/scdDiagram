import iter_ele
import asyncio
import time

def run(file):
    loop = asyncio.get_event_loop()
    
    task1 = asyncio.ensure_future(iter_ele.resolve(file,'IED'))
    task2 = asyncio.ensure_future(iter_ele.resolve(file,'AccessPoint'))
    task3 = asyncio.ensure_future(iter_ele.resolve(file,'LDevice'))
    task4 = asyncio.ensure_future(iter_ele.resolve(file,'DOI','DAI'))
    
    tasks = [task1,task2,task3,task4]
    loop.run_until_complete(tasks)

if __name__=='__main__':
    start = time.clock()
    run('HS220.scd')
    print('done!')
    end = time.clock()
    print('total time:',end-start,'s')