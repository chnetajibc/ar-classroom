import urllib.request
import json
import ssl

ssl._create_default_https_context = ssl._create_unverified_context

def search_hf(query):
    url = f"https://huggingface.co/api/models?search={query}&filter=gltf"
    try:
        req = urllib.request.Request(url)
        with urllib.request.urlopen(req) as response:
            data = json.loads(response.read().decode())
            for model in data[:3]:
                print(f"Found: {model['id']}")
    except Exception as e:
        print(f"Error: {e}")

search_hf("student")
search_hf("sitting")
search_hf("fan")
search_hf("window")
search_hf("door")
search_hf("light")
