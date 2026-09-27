(() => {
  'use strict';
  const tracks = JSON.parse(document.getElementById('track-data').textContent);
  const byId = new Map(tracks.map(track => [track.id, track]));
  const $ = id => document.getElementById(id);
  const audio = $('player-audio');
  const rows = [...document.querySelectorAll('.track-row')];
  const rowMap = new Map(rows.map(row => [row.dataset.id, row]));
  const storageKey = 'initial-d-season5-sound-room-v1';
  const state = {selected: tracks[0].id, category: 'all', library: 'all', sort: 'source', query: '', volume: .6};
  let favorites = new Set(), visible = tracks, playingRequest = 0, sessionOnly = false;
  try {
    const saved = JSON.parse(localStorage.getItem(storageKey) || '{}');
    if (byId.has(saved.selected)) state.selected = saved.selected;
    favorites = new Set((Array.isArray(saved.favorites) ? saved.favorites : []).filter(id => byId.has(id)));
    if (Number.isFinite(saved.volume)) state.volume = Math.max(0, Math.min(1, saved.volume));
  } catch (_) { sessionOnly = true; }
  function save() {
    try { localStorage.setItem(storageKey, JSON.stringify({selected:state.selected, favorites:[...favorites], volume:state.volume})); }
    catch (_) { sessionOnly = true; }
  }
  function icon(button, name) { button.querySelector('use').setAttribute('href', '#i-' + name); }
  function time(value) { const n = Math.max(0, Math.floor(Number.isFinite(value) ? value : 0)); return `${Math.floor(n/60)}:${String(n%60).padStart(2,'0')}`; }
  function normalize(value) { return value.normalize('NFKC').toLocaleLowerCase().replace(/\s+/g,' ').trim(); }
  function status(message) { $('playback-status').textContent = message; }
  function renderFavorites() {
    $('favorite-count').textContent = favorites.size;
    const selectedFavorite = favorites.has(state.selected);
    $('favorite').setAttribute('aria-pressed', String(selectedFavorite));
    $('favorite').setAttribute('aria-label', (selectedFavorite ? 'Remove song from' : 'Add song to') + ' favorites');
    rows.forEach(row => {
      const pressed = favorites.has(row.dataset.id);
      const button = row.querySelector('.row-favorite');
      button.setAttribute('aria-pressed', String(pressed));
      button.setAttribute('aria-label', `${pressed ? 'Remove' : 'Add'} ${byId.get(row.dataset.id).title} ${pressed ? 'from' : 'to'} favorites`);
    });
  }
  function filter() {
    const query = normalize(state.query);
    visible = tracks.filter(track => (state.category === 'all' || track.category === state.category)
      && (state.library !== 'favorites' || favorites.has(track.id))
      && (!query || normalize(`${track.title} ${track.artist} ${track.category} ${track.game || ''} ${(track.sourceCueNames || []).join(' ')}`).includes(query)));
    if (state.sort === 'title') visible.sort((a,b) => a.title.localeCompare(b.title));
    if (state.sort === 'duration') visible.sort((a,b) => a.duration-b.duration);
    const shown = new Set(visible.map(track => track.id));
    rows.forEach(row => { row.hidden = !shown.has(row.dataset.id); });
    visible.forEach(track => $('track-list').append(rowMap.get(track.id)));
    $('result-count').textContent = `${visible.length} ${visible.length === 1 ? 'track' : 'tracks'}`;
    $('list-title').textContent = state.library === 'favorites' ? 'Favorites' : state.category !== 'all' ? state.category : 'All tracks';
    $('empty').hidden = visible.length > 0;
    $('empty-title').textContent = state.library === 'favorites' && !query ? 'Your favorites start here' : 'No tracks found';
    $('empty-message').textContent = state.library === 'favorites' && !query ? 'Tap the heart on a song to keep it here.' : 'Try another title or artist.';
    $('clear-search').textContent = state.library === 'favorites' && !query ? 'Browse all tracks' : 'Clear filters';
    document.querySelectorAll('[data-library]').forEach(button => {
      const active = button.dataset.library === state.library && (button.dataset.library === 'favorites' || state.category === 'all');
      button.classList.toggle('active', active); button.setAttribute('aria-pressed', String(active));
    });
    document.querySelectorAll('[data-category]').forEach(button => {
      const active = button.dataset.category === state.category;
      button.classList.toggle('active', active);button.setAttribute('aria-pressed', String(active));
    });
    $('mobile-category').value = state.category;
    $('mobile-favorites').setAttribute('aria-pressed', String(state.library === 'favorites'));
    $('previous').disabled = visible.length === 0;
    $('next').disabled = visible.length === 0;
  }
  function renderPlayback() {
    const isPlaying = !audio.paused && !audio.ended;
    icon($('play'), isPlaying ? 'pause' : 'play');
    $('play').setAttribute('aria-label', isPlaying ? 'Pause preview' : 'Play preview');
    rows.forEach(row => {
      const selected = row.dataset.id === state.selected;
      row.classList.toggle('selected', selected);
      row.classList.toggle('playing', selected && isPlaying);
      row.querySelector('.row-info').setAttribute('aria-pressed', String(selected));
      icon(row.querySelector('.row-play'), selected && isPlaying ? 'pause' : 'play');
      row.querySelector('.row-play').setAttribute('aria-label', `${selected && isPlaying ? 'Pause' : 'Preview'} ${byId.get(row.dataset.id).title}`);
    });
  }
  function updateTime() {
    const length = Number.isFinite(audio.duration) ? audio.duration : byId.get(state.selected).previewDuration;
    const position = Math.min(length, Math.max(0, audio.currentTime || 0));
    $('seek').max = length; $('seek').value = position;
    $('seek').setAttribute('aria-valuetext', `${time(position)} of ${time(length)}`);
    $('elapsed').textContent = time(position);$('preview-end').textContent = time(length);
    const ratio = length > 0 ? position/length : 0;
    [...$('waveform').children].forEach((bar,index) => bar.classList.toggle('past', index/96 < ratio));
  }
  async function start() {
    const request = ++playingRequest;
    if (audio.ended || audio.currentTime >= audio.duration) audio.currentTime = 0;
    status('Loading preview…');
    try {
      await audio.play();
      if (request === playingRequest) {status('Playing preview');renderPlayback();}
    } catch (error) {
      if (request !== playingRequest || error.name === 'AbortError') return;
      status(error.name === 'NotAllowedError' ? 'Tap play to start the preview.' : 'Could not play this preview. Try opening the viewer in your browser.');
      renderPlayback();
    }
  }
  function choose(id, shouldPlay=false) {
    const track = byId.get(id); if (!track) return;
    ++playingRequest;audio.pause();
    state.selected = id;
    const fallback = document.getElementById('audio-' + id);
    audio.src = fallback.getAttribute('src');
    audio.load();
    $('cover').src = document.getElementById('art-' + id).getAttribute('src');
    $('cover').alt = track.category + ' cover';
    $('player-title').textContent = track.title;
    $('player-artist').textContent = track.artist || track.game;
    $('player-category').textContent = track.category;
    $('preview-badge').textContent = track.duration <= 30 ? 'FULL CLIP' : '30 SEC PREVIEW';
    document.querySelector('.cover-corner').textContent = `${String(tracks.indexOf(track)+1).padStart(2,'0')} / ${tracks.length}`;
    $('waveform').replaceChildren(...track.waveform.map(value => {
      const bar = document.createElement('i');bar.style.height = Math.max(3, value*34)+'px';return bar;
    }));
    renderFavorites();renderPlayback();updateTime();status('Ready to preview');save();
    if (shouldPlay) start();
  }
  function togglePlay() {
    if (!audio.paused && !audio.ended) {++playingRequest;audio.pause();status('Preview paused');}
    else start();
  }
  function skip(direction) {
    if (!visible.length) return;
    let index = visible.findIndex(track => track.id === state.selected);
    if (index < 0) index = direction > 0 ? -1 : 0;
    choose(visible[(index+direction+visible.length)%visible.length].id, !audio.paused && !audio.ended);
  }
  function toggleFavorite(id) {
    if (favorites.has(id)) favorites.delete(id);else favorites.add(id);
    save();renderFavorites();filter();
    if (sessionOnly) status('Favorites saved for this session.');
  }
  rows.forEach(row => {
    row.querySelector('.row-info').addEventListener('click', () => {
      if (state.selected !== row.dataset.id) choose(row.dataset.id, !audio.paused && !audio.ended);
    });
    row.querySelector('.row-play').addEventListener('click', () => {
      if (state.selected === row.dataset.id) togglePlay();else choose(row.dataset.id, true);
    });
    row.querySelector('.row-favorite').addEventListener('click', () => toggleFavorite(row.dataset.id));
    row.querySelector('.fallback-audio').addEventListener('play', event => {
      document.querySelectorAll('audio').forEach(other => { if (other !== event.target) other.pause(); });
    });
  });
  document.querySelectorAll('[data-category]').forEach(button => button.addEventListener('click', () => {
    state.category = button.dataset.category;state.library = 'all';filter();
  }));
  document.querySelectorAll('[data-library]').forEach(button => button.addEventListener('click', () => {
    state.library = button.dataset.library;state.category = 'all';filter();
  }));
  $('search').addEventListener('input', event => {state.query=event.target.value;filter();});
  $('sort').addEventListener('change', event => {state.sort=event.target.value;filter();});
  $('mobile-category').addEventListener('change', event => {state.category=event.target.value;state.library='all';filter();});
  $('mobile-favorites').addEventListener('click', () => {state.library=state.library==='favorites'?'all':'favorites';filter();});
  $('clear-search').addEventListener('click', () => {state.query='';state.category='all';state.library='all';$('search').value='';filter();});
  $('play').addEventListener('click',togglePlay);
  $('previous').addEventListener('click',()=>skip(-1));$('next').addEventListener('click',()=>skip(1));
  $('favorite').addEventListener('click',()=>toggleFavorite(state.selected));
  $('seek').addEventListener('input',event=>{
    if (audio.readyState>=1) {audio.currentTime=Number(event.target.value);updateTime();}
  });
  const updateVolume=()=>{icon($('mute'),audio.muted||audio.volume===0?'muted':'volume');$('mute').setAttribute('aria-label',audio.muted?'Unmute preview':'Mute preview');};
  $('volume').value=state.volume;audio.volume=state.volume;
  $('volume').addEventListener('input',event=>{state.volume=Number(event.target.value);audio.volume=state.volume;audio.muted=false;updateVolume();save();});
  $('mute').addEventListener('click',()=>{audio.muted=!audio.muted;updateVolume();});
  audio.addEventListener('timeupdate',updateTime);audio.addEventListener('loadedmetadata',updateTime);
  audio.addEventListener('play',renderPlayback);audio.addEventListener('pause',renderPlayback);
  audio.addEventListener('ended',()=>{renderPlayback();updateTime();status('Preview finished');});
  audio.addEventListener('error',()=>{status('Preview unavailable. Try opening the viewer in your browser.');renderPlayback();});
  document.addEventListener('keydown',event=>{
    if (event.ctrlKey||event.altKey||event.metaKey||event.target.closest('input,select,button,a,textarea,[contenteditable]'))return;
    if(event.code==='Space'){event.preventDefault();togglePlay();}
    if(event.code==='ArrowRight'){event.preventDefault();skip(1);}
    if(event.code==='ArrowLeft'){event.preventDefault();skip(-1);}
  });
  window.addEventListener('pagehide',()=>audio.pause());
  document.body.classList.add('js');
  filter();choose(state.selected);updateVolume();
})();
