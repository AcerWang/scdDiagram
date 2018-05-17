from SCD_element import resolve
from SCD_struct import resolve_struct

from multiprocessing.pool import Pool


def multi_task(file):
    
    pool = Pool(4)

    args = [(file,'IED'),(file,'AccessPoint'),(file,'LDevice'),(file,'LN','LDevice')]
    
    for arg in args:
        pool.apply_async(resolve,args=arg)
    
    pool.apply_async(resolve_struct,args=(file,))
    
    pool.close()

    pool.join()

    print('done!')

if __name__=='__main__':
    import time
    start = time.clock()
    multi_task('scd/ZTB.scd')
    end = time.clock()
    print('total time:',end-start,'s')