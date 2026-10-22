"""Call the locally configured official Blender MCP through its stdio client."""
import argparse
import asyncio
import json
import os
from pathlib import Path

from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

async def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('tool')
    parser.add_argument('--code-file')
    args = parser.parse_args()
    env = dict(os.environ)
    env.update(BLENDER_MCP_HOST='127.0.0.1', BLENDER_MCP_PORT='9876')
    client = Path.home() / '.codex/tools/blender-mcp-official-1.0.3/venv/Scripts/blender-mcp.exe'
    params = StdioServerParameters(command=str(client), env=env)
    payload = {'code': Path(args.code_file).read_text(encoding='utf-8')} if args.code_file else {}
    async with stdio_client(params) as (reader, writer):
        async with ClientSession(reader, writer) as session:
            await session.initialize()
            response = await session.call_tool(args.tool, payload)
            print(response.model_dump_json())
            if response.isError:
                raise SystemExit(1)

if __name__ == '__main__':
    asyncio.run(main())
