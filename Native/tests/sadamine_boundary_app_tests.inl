// Reproduce the reported roadside view with source driving and private saves.
// Both steering directions, three contact durations, bumper and chase views.
void prepareSadamineBoundaryFixture(App& app,unsigned scene){
    app.validationMode=true;app.replayPlaybackActive=false;app.multiplayer.active=false;app.paused=false;
    app.frontend.gameMode=original::OriginalGameMode::TimeAttack;app.frontend.course=10;app.frontend.car=0;app.frontend.automatic=true;
    app.frontend.reverse=app.reverse=app.frontend.night=app.night=app.frontend.wet=app.wet=false;
    app.start();app.loadingActive=app.vsActive=app.preRaceDialogueActive=false;app.menu=false;
    app.drivingView=(scene&1)?OriginalDrivingView::Chase:OriginalDrivingView::Bumper;
    unsigned running=0,contacts=0;const unsigned duration=std::array<unsigned,3>{80,160,540}.at(((scene-380)%6)/2);
    std::ofstream log(app.saveRoot.parent_path()/("boundary-"+std::to_string(scene)+".csv"));
    log<<"tick,x,y,z,lateral,width,wallContact,cameraLateral,cameraWidth\n";
    for(unsigned frame=0;frame<duration+600&&running<duration;++frame){
        DriverInput input;input.automatic=true;input.throttle=1;
        if(app.race.phase==RacePhase::Running){input.steer=scene<386?.8f:-.8f;++running;}
        app.simulate(input);
        contacts+=app.vehicle.wallContact;
        const auto p=app.projectRacePosition(app.vehicle.position);
        const auto eye=app.drivingView==OriginalDrivingView::Bumper?app.bumperCamera.frame().eye:app.originalCamera.frame().eye;
        const auto camera=app.projectRacePosition(eye);
        log<<running<<','<<app.vehicle.position.x<<','<<app.vehicle.position.y<<','<<app.vehicle.position.z<<','<<p.lateral<<','<<p.sample.width<<','<<app.vehicle.wallContact<<','<<camera.lateral<<','<<camera.sample.width<<'\n';
    }
    if(running!=duration||!contacts)throw std::runtime_error("Sadamine roadside fixture did not reach wall contact");
    app.paused=true;
    const auto words=app.presentedSession().vehicle().drive.words;
    Idas3UiBeginFrame(app.renderer.width,app.renderer.height);
    if(!app.render(0)||words!=app.presentedSession().vehicle().drive.words)
        throw std::runtime_error("Sadamine roadside render failed or changed driving state");
}
