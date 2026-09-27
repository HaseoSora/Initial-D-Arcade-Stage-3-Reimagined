using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

public static class Idas3WheelRigChecks
{
    public static void Run(){
        string root=Path.GetFullPath("Verification/discord-bugs-20260927/rig-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(root);
        int checks=0;void Check(bool ok,string why){++checks;if(!ok)throw new Exception(why);}
        var mapper=new Idas3ControlBindings();mapper.Initialize(root);
        var steer=new Idas3ControllerControl{path="stick/x",label="Steering",value=0};
        var gas=new Idas3ControllerControl{path="stick/x",label="Gas",value=1};
        var brake=new Idas3ControllerControl{path="stick/y",label="Brake",value=1};
        var shift=new Idas3ControllerControl{path="button1",label="Shift",minimum=0,button=true};
        void Bind(string key,Idas3ControlBindings.ActionId action,Idas3ControllerControl control,int direction,float rest){
            mapper.SelectControllerProfile(key,key,true);Check(mapper.TrySetDraftControl(action,control,direction,rest)&&mapper.ApplyDraft(),"Save rig binding "+key);
        }
        Bind("pedals",Idas3ControlBindings.ActionId.Accelerate,gas,-1,1);Bind("pedals",Idas3ControlBindings.ActionId.Brake,brake,-1,1);
        Bind("shifter",Idas3ControlBindings.ActionId.ShiftUp,shift,1,0);
        Bind("wheel",Idas3ControlBindings.ActionId.SteerLeft,steer,-1,0);Bind("wheel",Idas3ControlBindings.ActionId.SteerRight,steer,1,0);
        // Reload saved profiles, as on next launch; duplicate path names must
        // resolve against the correct physical device rather than collide.
        mapper=new Idas3ControlBindings();mapper.Initialize(root);mapper.SelectControllerProfile("wheel","wheel",true);
        var pedals=new Idas3ControllerDevices.RigSample{key="physical-pedals",profile="pedals",controls=new[]{gas,brake}};
        var shifter=new Idas3ControllerDevices.RigSample{key="physical-shifter",profile="shifter",controls=new[]{shift}};
        var samples=new[]{pedals,shifter};
        Idas3Native.FrameInput Poll(Idas3ControllerDevices.RigSample[] rig){mapper.Poll(_=>false,new Idas3ControlBindings.PadState{connected=true},0,new[]{steer},rig);var f=new Idas3Native.FrameInput();mapper.ApplyDriving(ref f);return f;}
        Poll(samples);steer.value=.5f;gas.value=-1;shift.value=1;
        var packet=Poll(samples);Check(packet.rightTrigger==255&&packet.thumbLX>16000&&(packet.padButtons&0x2000)!=0,"Separate wheel, gas and shifter did not work simultaneously");
        Check(packet.leftTrigger==0,"Unpressed brake was applied");brake.value=-1;packet=Poll(samples);Check(packet.leftTrigger==255,"Separate brake was lost");
        packet=Poll(new[]{shifter});Check(packet.rightTrigger==0&&packet.leftTrigger==0&&packet.thumbLX>16000,"Unplugged pedals left stale input or stopped steering");
        packet=Poll(samples);Check(packet.rightTrigger==0&&packet.leftTrigger==0,"Reconnected held pedals bypassed release guard");
        gas.value=1;Poll(samples);gas.value=-1;packet=Poll(samples);Check(packet.rightTrigger==255&&packet.leftTrigger==0,"Gas waited for unrelated held brake release");
        mapper.SelectControllerProfile("gamepad","Gamepad",false);packet=Poll(samples);Check(packet.rightTrigger==0,"Wheel rig hijacked selected gamepad");
        mapper.SelectControllerProfile("wheel","wheel",true);gas.value=brake.value=1;shift.value=0;Poll(samples);
        gas.value=-1;Poll(samples);var menu=new Idas3Native.FrameInput();mapper.ApplyMenu(ref menu,true);Check((menu.key0&(1u<<13))!=0,"Separate pedal cannot confirm a menu");
        mapper.BeginCapture(Idas3ControlBindings.ActionId.Camera,Idas3ControlBindings.Slot.Controller,0);Poll(samples);shift.value=1;Poll(samples);Check(mapper.IsCapturing,"Capture selected an unrelated shifter");mapper.CancelCapture();
        InputSystem.RegisterLayout("{\"name\":\"Idas3RigCheckJoystick\",\"extend\":\"Joystick\"}",name:"Idas3RigCheckJoystick",matches:new InputDeviceMatcher().WithInterface("Idas3RigCheck"));
        Joystick Add(string name)=>(Joystick)InputSystem.AddDevice(new InputDeviceDescription{interfaceName="Idas3RigCheck",manufacturer="Private rig check",product=name,serial=name});
        var wheelDevice=Add("wheel");var pedalDevice=Add("pedals");var shifterDevice=Add("shifter");
        uint NoPad(uint slot,out Idas3Native.PadState value){value=default;return 1167;}
        try{using(var devices=new Idas3ControllerDevices(()=>0,NoPad,d=>d.description.interfaceName=="Idas3RigCheck")){
            devices.Initialize(Path.Combine(root,"discovery"));string wheelKey=null,pedalKey=null;
            foreach(var choice in devices.Choices){if(choice.label.StartsWith("wheel"))wheelKey=choice.key;if(choice.label.StartsWith("pedals"))pedalKey=choice.key;}
            Check(devices.Select(wheelKey),"Select synthetic wheel");
            InputSystem.QueueDeltaStateEvent(pedalDevice.stick,new Vector2(.8f,0));InputSystem.Update();devices.Tick(true);
            Check(devices.SelectedKey==wheelKey&&devices.RigSamples.Count==2,"Independent pedal and shifter were not sampled beside selected wheel");
            devices.Select("automatic");devices.Select(wheelKey);devices.Select("automatic");
            InputSystem.QueueDeltaStateEvent(pedalDevice.stick,new Vector2(-.8f,0));InputSystem.Update();devices.Tick(true);
            Check(devices.ActiveName.StartsWith("wheel"),"Pedal motion stole automatic rig selection");
            devices.Select("keyboard");devices.Tick(true);Check(devices.RigSamples.Count==0&&!devices.TryRead(out _),"Keyboard only retained wheel rig input");
            devices.Select(wheelKey);devices.Tick(true);InputSystem.RemoveDevice(pedalDevice);devices.Tick(false);
            Check(devices.RigSamples.Count==1,"Disconnected auxiliary device left a live sample");
        }}finally{
            if(wheelDevice.added)InputSystem.RemoveDevice(wheelDevice);if(pedalDevice.added)InputSystem.RemoveDevice(pedalDevice);if(shifterDevice.added)InputSystem.RemoveDevice(shifterDevice);
            InputSystem.RemoveLayout("Idas3RigCheckJoystick");
        }
        File.WriteAllText(Path.Combine(root,"PASS.txt"),checks+" isolated multi-device binding and Unity discovery checks passed");Debug.Log("PASS "+checks+" wheel rig checks");
    }
}
