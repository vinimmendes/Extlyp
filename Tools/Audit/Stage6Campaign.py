"""Drive only the three isolated stage-6 players; preserve all commands/results."""
import json,pathlib,time,sys
OUT=pathlib.Path(__file__).resolve().parents[2]/'Logs/auditoria/2026-10-16_083542_382c7a/etapa6'
def state(role):
    try: return json.loads((OUT/role/'state.json').read_text(encoding='utf-8-sig'))
    except (ValueError,FileNotFoundError): return {}
def wait(pred, seconds=50):
    end=time.monotonic()+seconds
    while time.monotonic()<end:
        value=pred()
        if value:return value
        time.sleep(.3)
    raise TimeoutError('Campaign condition not reached')
def command(role,action,**kwargs):
    folder=OUT/role; folder.mkdir(exist_ok=True)
    uid=str(time.time_ns())+'-'+action
    temp=folder/'command.tmp'; temp.write_text(json.dumps(dict(id=uid,action=action,**kwargs)),encoding='utf-8'); temp.rename(folder/'command.json')
    result=folder/(uid+'-result.json')
    wait(lambda:result.exists(),20)
    data=json.loads(result.read_text(encoding='utf-8-sig'))
    if data.get('error'):raise RuntimeError(data['error'])
    return data
def save(name,data): (OUT/(name+'.json')).write_text(json.dumps(data,indent=2),encoding='utf-8')
def human(s,owner=None): return next(p for p in s['players'] if p['human'] and (p['owner']==owner if owner is not None else p['local']))
def campaign():
    wait(lambda:all(state(r) for r in ['server','client1','client2']),90)
    for r in ['client1','client2']:command(r,'camp')
    wait(lambda:all(state(r).get('synchronized') and state(r).get('connected') for r in ['client1','client2']),90)
    save('camp-connected',{r:state(r) for r in ['server','client1','client2']})
    command('client1','capture')
    for r in ['client1','client2']: command(r,'expedition')
    wait(lambda:sum(p['human'] for p in state('server').get('players',[]))==2,90)
    # Use the production matchmaking countdown rather than bypassing start guards.
    wait(lambda:state('server').get('state')=='Running',70)
    save('match-start',{r:state(r) for r in ['server','client1','client2']})
    command('server','freeze')
    command('client1','capture'); command('client2','capture')
    a=human(state('client1')); b=human(state('client2'))
    save('spawn-unique',{'distinct':(a['x'],a['y'])!=(b['x'],b['y']),'first':a,'second':b})
    command('client1','fire',angle=90); time.sleep(1)
    save('after-fire',{r:state(r) for r in ['server','client1']})
    command('client1','reload'); time.sleep(4)
    save('after-reload',{r:state(r) for r in ['server','client1']})
    command('server','add',client=a['owner'],value=3); time.sleep(1)
    command('client1','drop',value=0); time.sleep(1)
    save('after-drop',{r:state(r) for r in ['server','client1']})
    a=human(state('client1'))
    command('server','chest',x=a['x']+1,y=a['y']); time.sleep(2)
    save('closed-chest',{r:state(r) for r in ['server','client1','client2']})
    command('client1','capture')
    command('client1','aim'); time.sleep(1)
    save('aim-gamepad-and-mouse',{r:state(r) for r in ['server','client1']})
    command('client1','input',value=1); time.sleep(1)
    command('client1','input',value=0); time.sleep(.5)
    save('after-move',{r:state(r) for r in ['server','client1']})
    command('client1','bag'); command('client1','capture'); command('client1','bag')
    command('server','bank',client=a['owner']); time.sleep(1)
    save('after-bank',{r:state(r) for r in ['server','client1']})
    command('server','damage',client=b['owner'],value=10000); time.sleep(1)
    save('after-death',{r:state(r) for r in ['server','client2']})
    for r in ['client1','client2']: command(r,'disconnect')
    time.sleep(2)
    for r in ['client1','client2']: command(r,'camp')
    wait(lambda:all(state(r).get('synchronized') and state(r).get('connected') for r in ['client1','client2']),90)
    save('reconnected',{r:state(r) for r in ['server','client1','client2']})
    save('campaign-completion',{'completed':True,'time':time.time(),'limits':'Administrative freeze/add/bank/damage; no fault injection or Android validation'})
if __name__=='__main__':
    try: campaign()
    except Exception as e:
        save('campaign-error',{'error':str(e),'states':{r:state(r) for r in ['server','client1','client2']}})
        raise
