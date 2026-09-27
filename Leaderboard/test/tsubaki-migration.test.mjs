import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {DatabaseSync} from 'node:sqlite';
test('Tsubaki migration preserves previous times, moderation, and both replay storage formats',()=>{
 const db=new DatabaseSync(':memory:');db.exec('PRAGMA foreign_keys=ON');
 for(const name of ['0001_leaderboard','0002_imported_times','0003_replays','0004_replay_chunks','0005_enna_skyline','0006_special_stage_courses'])db.exec(readFileSync(new URL('../migrations/'+name+'.sql',import.meta.url),'utf8'));
 db.exec("INSERT INTO devices VALUES ('p','token',1,1); INSERT INTO runs VALUES ('r','p','rules',2,29,1,0,1200000,'[1]','[2]',1,1,999999,'old',1,1,'moderated',0,44,'hash'); INSERT INTO replays VALUES ('r',zeroblob(44)); INSERT INTO replay_chunks VALUES ('r',0,x'01020304');");
 const before={};for(const table of ['runs','replays','replay_chunks','devices','settings'])before[table]=db.prepare('SELECT * FROM '+table).all();
 db.exec('BEGIN');db.exec(readFileSync(new URL('../migrations/0007_tsubaki_line.sql',import.meta.url),'utf8'));db.exec('COMMIT');
 for(const table of Object.keys(before))assert.deepEqual(db.prepare('SELECT * FROM '+table).all(),before[table]);
 assert.deepEqual(db.prepare('PRAGMA foreign_key_check').all(),[]);
 assert.equal(db.prepare("SELECT type FROM sqlite_schema WHERE name='runs'").get().type,'view');
 for(const table of ['replays','replay_chunks'])assert.equal(db.prepare('PRAGMA foreign_key_list('+table+')').get().table,'runs_storage');
 db.exec('UPDATE runs SET condition=30');db.exec('UPDATE runs SET condition=31');assert.throws(()=>db.exec('UPDATE runs SET condition=32'));
 db.exec("UPDATE runs SET hidden=0,reason='restored'");assert.equal(db.prepare('SELECT condition FROM runs').get().condition,31);
 db.exec('UPDATE runs SET condition=29');assert.equal(db.prepare('SELECT condition_extended FROM runs_storage').get().condition_extended,null);
 db.exec('DELETE FROM replays;DELETE FROM runs');assert.equal(db.prepare('SELECT count(*) n FROM replay_chunks').get().n,0);db.close();
});
