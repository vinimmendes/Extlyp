"""Publish release artifacts with Git's existing GitHub credential; never log secrets."""
import argparse, json, os, subprocess, urllib.request, urllib.error
from pathlib import Path

REPO='vinimmendes/Extlyp'
def credential():
    token=os.environ.get('GH_TOKEN') or os.environ.get('GITHUB_TOKEN')
    if token: return token
    p=subprocess.run(['git','credential','fill'],input='protocol=https\nhost=github.com\npath='+REPO+'.git\n\n',text=True,capture_output=True,check=True)
    fields=dict(line.split('=',1) for line in p.stdout.splitlines() if '=' in line)
    if not fields.get('password'): raise RuntimeError('GitHub credential is unavailable')
    return fields['password']
def main():
    a=argparse.ArgumentParser(); a.add_argument('mode',choices=['inspect','download','publish']); a.add_argument('--tag'); a.add_argument('--commit'); a.add_argument('--notes'); a.add_argument('--asset',action='append',default=[]); a.add_argument('--destination'); a.add_argument('--asset-name'); args=a.parse_args()
    token=credential()
    def request(path,method='GET',data=None,content_type='application/json'):
        url=path if path.startswith('https://') else 'https://api.github.com/repos/'+REPO+path
        if not url.startswith(('https://api.github.com/','https://uploads.github.com/')): raise RuntimeError('Unexpected GitHub endpoint')
        raw=json.dumps(data).encode() if isinstance(data,dict) else data
        req=urllib.request.Request(url,data=raw,method=method,headers={'Authorization':'Bearer '+token,'Accept':'application/vnd.github+json','X-GitHub-Api-Version':'2022-11-28','User-Agent':'Extlyp-release','Content-Type':content_type})
        with urllib.request.urlopen(req,timeout=180) as response:
            payload=response.read(); return json.loads(payload) if payload else None
    if args.mode=='inspect':
        releases=request('/releases?per_page=5')
        print(json.dumps([{'tag':r['tag_name'],'draft':r['draft'],'url':r['html_url'],'assets':[{'id':x['id'],'name':x['name'],'size':x['size']} for x in r['assets']]} for r in releases],indent=2))
    elif args.mode=='download':
        r=request('/releases/tags/'+args.tag)
        assets=[x for x in r['assets'] if x['name']==args.asset_name] if args.asset_name else [x for x in r['assets'] if x['name'].endswith('.apk')]
        if len(assets)!=1 or not args.destination: raise RuntimeError('Expected one APK and a local destination')
        req=urllib.request.Request(assets[0]['url'],headers={'Authorization':'Bearer '+token,'Accept':'application/octet-stream','User-Agent':'Extlyp-release'})
        class Redirect(urllib.request.HTTPRedirectHandler):
            def redirect_request(self,request,fp,code,msg,headers,newurl):
                result=super().redirect_request(request,fp,code,msg,headers,newurl)
                from urllib.parse import urlsplit
                if result and urlsplit(newurl).hostname!=urlsplit(request.full_url).hostname: result.remove_header('Authorization')
                return result
        with urllib.request.build_opener(Redirect()).open(req,timeout=180) as response,open(args.destination,'wb') as output:
            while chunk:=response.read(1024*1024): output.write(chunk)
        print(json.dumps({'downloaded':args.destination,'bytes':Path(args.destination).stat().st_size}))
    else:
        if not args.tag or not args.commit or not args.notes or not args.asset: raise RuntimeError('Tag, commit, notes and assets required')
        try: existing=request('/releases/tags/'+args.tag)
        except urllib.error.HTTPError as e:
            if e.code!=404: raise
            existing=None
        if existing and not existing['draft']: raise RuntimeError('Published release already exists; never replace it implicitly')
        r=existing or request('/releases','POST',{'tag_name':args.tag,'target_commitish':args.commit,'name':'Cyber Extraction '+args.tag,'body':Path(args.notes).read_text(encoding='utf-8'),'draft':True,'prerelease':False})
        names={x['name'] for x in r['assets']}
        from urllib.parse import quote
        for name in args.asset:
            p=Path(name)
            if p.name in names: raise RuntimeError('Asset already present in draft: '+p.name)
            # GitHub receives the exact local bytes. No tokens or profiles enter the artifacts.
            with p.open('rb') as file: request(r['upload_url'].split('{',1)[0]+'?name='+quote(p.name),'POST',file.read(),'application/vnd.android.package-archive' if p.suffix=='.apk' else 'application/octet-stream')
            print('Uploaded '+p.name,flush=True)
        r=request('/releases/'+str(r['id']),'PATCH',{'draft':False})
        print(json.dumps({'url':r['html_url'],'tag':r['tag_name'],'assets':[{'name':x['name'],'url':x['browser_download_url'],'size':x['size']} for x in r['assets']]},indent=2))
if __name__=='__main__':
    try: main()
    except urllib.error.HTTPError as error: raise SystemExit('GitHub HTTP '+str(error.code)+' '+error.reason)
