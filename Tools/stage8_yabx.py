import struct
from pathlib import Path
class Reader:
    def __init__(self,b):self.b=b;self.at=0
    def take(self,n):
        a=self.at;self.at+=n
        if n<0 or self.at>len(self.b):raise ValueError(('bounds',a,n))
        return self.b[a:self.at]
    def u8(self):return self.take(1)[0]
    def u16(self):return struct.unpack('<H',self.take(2))[0]
    def u32(self):return struct.unpack('<I',self.take(4))[0]
    def string(self):
        n=self.u8()
        if n==255:n=self.u32()
        if n==0:return ''
        b=self.take(n);assert b[-1]==0,(self.at,n,b)
        return b[:-1].decode('utf8',errors='replace')
def schema(b):
    r=Reader(b);r.at=b.index(b'\x11yabukita::Object\0');classes=[None]
    while True:
        name=r.string()
        if not name:break
        flag=r.u8();parent=r.u16();fields=[]
        while True:
            field=r.string()
            if not field:break
            kind=r.u8();size=r.u16();fields.append((field,kind,size))
        assert parent<len(classes),(name,parent)
        classes.append(dict(name=name,flag=flag,parent=parent,fields=fields))
    return r,classes
def load(path):
    b=Path(path).read_bytes();r,classes=schema(b)
    assert r.string()=='' and r.string()=='' # external references / named object map
    count=r.u16();objects=[]
    for i in range(count):
        cid=r.u16();size=r.u32();end=r.at+size;fields={};c=cid
        while c:
            for name,kind,n in classes[c]['fields']:
                if classes[c]['name']=='_sSerial::_sTextureImage' and name=='file':n=0
                fields[name]=r.take(n if n else r.u32())
            c=classes[c]['parent']
        assert r.at==end,(i,classes[cid]['name'],r.at,end)
        objects.append(dict(type=classes[cid]['name'].split('::')[-1],fields=fields))
    assert not any(b[r.at:]),(r.at,len(b))
    return objects
def value(obj,key,fmt='I'):
    b=obj['fields'][key];return struct.unpack('<'+fmt,b)[0]
def string(obj,key='name'):
    b=obj['fields'][key];n=struct.unpack_from('<H',b)[0]
    assert n+2==len(b);return b[2:].rstrip(b'\0').decode('utf8',errors='replace')
if __name__=='__main__':
    import sys
    b=Path(sys.argv[1]).read_bytes();r,c=schema(b)
    for i,v in enumerate(c):print(i,v)
    print('schema end',hex(r.at))
    for o in range(r.at,r.at+512,16):print(hex(o),b[o:o+16].hex(' '),repr(b[o:o+16]))
