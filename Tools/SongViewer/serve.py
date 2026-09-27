"""Loopback-only preview server with byte-range support for audio seeking."""
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import re

ROOT=Path(__file__).resolve().parents[2]/'Verification/arcade-s5-music-audit-20260926/Viewer'

class Handler(SimpleHTTPRequestHandler):
    def send_head(self):
        self.audio_range=None
        path=Path(self.translate_path(self.path))
        if path.is_dir():return super().send_head()
        if not path.resolve().is_relative_to(ROOT.resolve()):
            self.send_error(403);return None
        requested=self.headers.get('Range')
        if not requested or not path.is_file():return super().send_head()
        match=re.fullmatch(r'bytes=(\d*)-(\d*)',requested)
        size=path.stat().st_size
        try:
            if not match or not any(match.groups()):raise ValueError()
            left,right=match.groups()
            start=int(left) if left else max(0,size-int(right))
            end=min(size-1,int(right)) if left and right else size-1
            if start>end or start>=size:raise ValueError()
        except ValueError:
            self.send_response(416);self.send_header('Content-Range',f'bytes */{size}');self.end_headers();return None
        file=path.open('rb');file.seek(start);self.audio_range=end-start+1
        self.send_response(206)
        self.send_header('Content-Type',self.guess_type(str(path)))
        self.send_header('Content-Length',str(self.audio_range))
        self.send_header('Content-Range',f'bytes {start}-{end}/{size}')
        self.end_headers();return file
    def end_headers(self):
        self.send_header('Accept-Ranges','bytes')
        self.send_header('Cache-Control','no-cache')
        super().end_headers()
    def copyfile(self,source,outputfile):
        try:
            if self.audio_range is None:return super().copyfile(source,outputfile)
            remaining=self.audio_range
            while remaining:
                chunk=source.read(min(65536,remaining))
                if not chunk:break
                outputfile.write(chunk);remaining-=len(chunk)
        except (BrokenPipeError,ConnectionResetError):pass

if __name__=='__main__':
    server=ThreadingHTTPServer(('127.0.0.1',8769),partial(Handler,directory=str(ROOT)))
    server.serve_forever()
