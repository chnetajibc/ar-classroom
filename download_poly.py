import urllib.request
import re
import ssl

ssl._create_default_https_context = ssl._create_unverified_context

def download_model(url_path, filename):
    url = f"https://poly.pizza{url_path}"
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    try:
        with urllib.request.urlopen(req) as response:
            html = response.read().decode('utf-8')
            # Look for the download URL in the JSON state
            import json
            match = re.search(r'window\.__SERVER_APP_STATE__ = (.*?)</script>', html)
            if match:
                state = json.loads(match.group(1))
                if 'model' in state['initialData']:
                    model = state['initialData']['model']
                    if 'PublicID' in model:
                        pub_id = model['PublicID']
                        print(f"Found model ID: {pub_id}")
    except Exception as e:
        print(f"Failed: {e}")

download_model("/m/dPExITRhhZ", "fan.glb")
