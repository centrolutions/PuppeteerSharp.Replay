#!/bin/bash
package_content=$(curl -s https://api.nuget.org/v3/registration5-semver1/puppeteersharp.replay/index.json)
package_version=$(echo $package_content | python3 -c 'import json,sys;obj=json.load(sys.stdin);print(obj["items"][0]["items"][0]["catalogEntry"]["version"])')
echo ${package_version} | awk -F. '{print $1"."$2"."$3+1}'
