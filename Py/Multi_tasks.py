from multiprocessing.pool import Pool
from multiprocessing import cpu_count

from SCD_element import resolve
from SCD_struct import resolve_struct
from Data_process import getRelation
from DBHelper import DataBase


def multi_task(file):

    pool = Pool(cpu_count())

    args = [(file,'IED'),(file,'LDevice'),(file,'LN','LDevice'),(file,'ExtRef','LN')]
    
    for arg in args:
        pool.apply_async(resolve,args=arg)
    
    pool.apply_async(resolve_struct,args=(file,))
    
    pool.close()

    pool.join()
    
    db = DataBase()
    getRelation(db)
    db.close_connection()
    
    print('done!')

if __name__=='__main__':
    import time
    start = time.clock()
    
    db = DataBase()
    
    db.delete('IEDTree')
    db.delete('IED')
    db.delete('LDevice')
    db.delete('LN')
    db.delete('ExtRef')
    
    db.close_connection()

    multi_task('../scd/BXB.scd')
    end = time.clock()
    print('total time:',end-start,'s')
	input('...')
