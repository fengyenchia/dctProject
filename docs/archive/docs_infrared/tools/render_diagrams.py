"""Render this folder's data to matching Mermaid, SVG, PNG and Markdown.
Usage: python tools/render_diagrams.py (run from this scheme folder)
Requires Pillow and Microsoft JhengHei on Windows, or Noto Sans CJK TC.
"""
from pathlib import Path
from html import escape
import json, math, os
from PIL import Image, ImageDraw, ImageFont
ROOT = Path(__file__).resolve().parents[1]
MODE = "紅外線"
PALETTE = {
    "normal": ("#e8f1fd", "#7099c9"),
    "device": ("#e5f5f1", "#459489"),
    "cloud": ("#eee7fb", "#9a7ac7"),
    "warning": ("#fff0df", "#cc9154"),
    "cancel": ("#fdeceb", "#c58b86"),
    "success": ("#e7f4e9", "#69a977"),
}
FONT_CANDIDATES = [os.environ.get("DIAGRAM_FONT", ""),
    "C:/Windows/Fonts/msjh.ttc", "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc"]
FONT_PATH = next((p for p in FONT_CANDIDATES if p and Path(p).is_file()), None)
if FONT_PATH is None: raise RuntimeError("Set DIAGRAM_FONT to a CJK font file.")
def font(size): return ImageFont.truetype(FONT_PATH, size)
def render(spec):
    width,height=spec["size"]; bg="#f7f9fc"; ink="#24334b"; edge="#60738b"
    im=Image.new("RGB",(width,height),bg); draw=ImageDraw.Draw(im)
    svg=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">',
         f'<rect width="{width}" height="{height}" fill="{bg}"/>']
    nodes={n["id"]:n for n in spec["nodes"]}
    def label(x,y,text,size=22,anchor="middle",color=ink):
        draw.text((x,y),text,font=font(size),fill=color,anchor="mm" if anchor=="middle" else "lm")
        svg.append(f'<text x="{x}" y="{y}" dominant-baseline="central" font-family="Microsoft JhengHei, Noto Sans CJK TC, sans-serif" font-size="{size}" fill="{color}" text-anchor="{anchor}">{escape(text)}</text>')
    def port(key,side):
        n=nodes[key];x,y=n["pos"];w,h=n.get("size",[320,120])
        return {"L":(x,y+h/2),"R":(x+w,y+h/2),"T":(x+w/2,y),"B":(x+w/2,y+h)}[side]
    for e in spec["edges"]:
        origin=port(e["from"],e.get("start","B"))
        offset=e.get("start_offset",[0,0])
        origin=(origin[0]+offset[0],origin[1]+offset[1])
        pts=[origin]+[tuple(p)for p in e.get("via",[])]+[port(e["to"],e.get("end","T"))]
        dashed=e.get("dashed",False)
        svg.append('<polyline points="'+' '.join(f'{x},{y}'for x,y in pts)+f'" fill="none" stroke="{edge}" stroke-width="2.2" stroke-linejoin="round"'+(' stroke-dasharray="8 6"' if dashed else '')+'/>')
        for a,b in zip(pts,pts[1:]):
            dx,dy=b[0]-a[0],b[1]-a[1];dist=math.hypot(dx,dy)
            if dashed and dist:
                for at in range(0,int(dist),14):
                    stop=min(at+8,dist)
                    draw.line((a[0]+dx*at/dist,a[1]+dy*at/dist,a[0]+dx*stop/dist,a[1]+dy*stop/dist),fill=edge,width=2)
            elif not dashed: draw.line((a,b),fill=edge,width=2)
        a,b=pts[-2:];angle=math.atan2(b[1]-a[1],b[0]-a[0])
        tip=[b,(b[0]-12*math.cos(angle-.4),b[1]-12*math.sin(angle-.4)),(b[0]-12*math.cos(angle+.4),b[1]-12*math.sin(angle+.4))]
        draw.polygon(tip,fill=edge)
        svg.append('<polygon points="'+' '.join(f'{x:.2f},{y:.2f}'for x,y in tip)+f'" fill="{edge}"/>')
        if e.get("both"):
            a,b=pts[1],pts[0];angle=math.atan2(b[1]-a[1],b[0]-a[0])
            tip2=[b,(b[0]-12*math.cos(angle-.4),b[1]-12*math.sin(angle-.4)),(b[0]-12*math.cos(angle+.4),b[1]-12*math.sin(angle+.4))]
            draw.polygon(tip2,fill=edge)
            svg.append('<polygon points="'+' '.join(f'{x:.2f},{y:.2f}'for x,y in tip2)+f'" fill="{edge}"/>')
        if e.get("label"):
            x,y=e.get("label_pos",[(pts[0][0]+pts[-1][0])/2,(pts[0][1]+pts[-1][1])/2-16])
            text=e["label"];w=draw.textlength(text,font=font(18))+16
            draw.rounded_rectangle((x-w/2,y-15,x+w/2,y+15),radius=4,fill=bg)
            svg.append(f'<rect x="{x-w/2}" y="{y-15}" width="{w}" height="30" rx="4" fill="{bg}"/>')
            label(x,y,text,18,color="#4d6079")
    for n in spec["nodes"]:
        x,y=n["pos"];w,h=n.get("size",[320,120]);fill,stroke=PALETTE[n.get("role","normal")]
        if n.get("shape")=="decision":
            pts=[(x+w/2,y),(x+w,y+h/2),(x+w/2,y+h),(x,y+h/2)]
            draw.polygon(pts,fill=fill);draw.line(pts+[pts[0]],fill=stroke,width=2)
            svg.append('<polygon points="'+' '.join(f'{a},{b}'for a,b in pts)+f'" fill="{fill}" stroke="{stroke}" stroke-width="2"/>')
        else:
            draw.rounded_rectangle((x,y,x+w,y+h),radius=15,fill=fill,outline=stroke,width=2)
            svg.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="15" fill="{fill}" stroke="{stroke}" stroke-width="2"/>')
        size=n.get("font_size",22);lines=[]
        for part in n["text"].split("\n"):
            line=""
            for c in part:
                if draw.textlength(line+c,font=font(size))>w-30 and line:lines.append(line);line=c
                else:line+=c
            lines.append(line)
        if len(lines)*30>h-12: raise ValueError(f'{spec["id"]}:{n["id"]} text exceeds its box')
        for i,line in enumerate(lines):label(x+w/2,y+h/2+(i-(len(lines)-1)/2)*30,line,size)
    label(65,60,spec["title"],34,"start")
    label(65,104,spec["subtitle"],18,"start", "#66758b")
    label(65,height-32,f"{MODE} 技術資料／事件流程｜箭頭為資料與事件；參數與驗證見待確認清單",17,"start","#758299")
    svg.append('</svg>')
    out=ROOT/"assets"/"技術流程圖";out.mkdir(parents=True,exist_ok=True)
    (out/(spec["id"]+".svg")).write_text("\n".join(svg)+"\n",encoding="utf-8")
    im.save(out/(spec["id"]+".png"))
    mermaid=["flowchart TD"]
    for n in spec["nodes"]:
        text=n["text"].replace("\n","／").replace('"',"'")
        mermaid.append(f'  {n["id"]}'+('{"'+text+'"}' if n.get("shape")=="decision" else '["'+text+'"]'))
    for e in spec["edges"]:
        arrow="<-->" if e.get("both") else ("-.->" if e.get("dashed") else "-->");lab='|"'+e["label"].replace('"',"'")+'"|' if e.get("label")else""
        mermaid.append(f'  {e["from"]} {arrow}{lab} {e["to"]}')
    for n in spec["nodes"]:
        fill,stroke=PALETTE[n.get("role","normal")]
        mermaid.append(f'  style {n["id"]} fill:{fill},stroke:{stroke}')
    (out/(spec["id"]+".mmd")).write_text("\n".join(mermaid)+"\n",encoding="utf-8")
    return len(nodes),len(spec["edges"])
def notes_markdown(rows):
    if isinstance(rows, str):
        return rows + "\n\n"
    lines = ["| 閱讀重點 | 內容 |", "| --- | --- |"]
    lines.extend("| " + label + " | " + detail + " |" for label, detail in rows)
    return "\n".join(lines) + "\n\n"

if __name__=="__main__":
    specs=json.loads((ROOT/"assets"/"技術流程圖"/"diagram_data.json").read_text(encoding="utf-8"))
    for spec in specs:print(spec["id"],render(spec))
    version=specs[0]["version"]
    parts=[f"# 音虫 {MODE}｜技術資料與事件流程圖\n\n日期：2026-11-08｜版本：{version}\n\n",
        "- 依[技術架構](技術架構.md)與[開發規格](開發規格.md)繪製：技術資料／事件流程，非觀眾任務圖。\n- 從各圖左上起點沿箭頭讀，順序為 **01 → 02 → 03 → 04 → 05**。\n\n",
        notes_markdown([["箭頭", "實線：資料／事件；虛線：重試／持續觀測；雙向：來回通訊。"], ["Unity／Gateway", "Unity：唯一遊戲狀態；Gateway：電腦上的本機串接服務。"], ["群落／SAN", "群落為一隻或一群，整群算一次捕捉；SAN 為遊戲精神值。"], ["outbox／token", "outbox：待上傳佇列；token：唯讀成果網址代碼。"]]),
        notes_markdown(specs[0]["intro"]),
        "未定參數見[待確認清單](待確認與驗證清單.md)，跨方案核對見[功能覆蓋](方案對照與功能覆蓋.md)。圖為規劃，非實機驗證。\n\n"]
    out=ROOT/"assets"/"技術流程圖"
    for index,spec in enumerate(specs,1):
        ident=spec["id"]
        parts.extend([f'## {index}. {spec["title"][4:].strip()}\n\n',notes_markdown(spec["caption"]),
            f'![{spec["title"]}](assets/技術流程圖/{ident}.png)\n\n',
            f'[SVG](assets/技術流程圖/{ident}.svg) · [PNG](assets/技術流程圖/{ident}.png) · [Mermaid 原始碼](assets/技術流程圖/{ident}.mmd)\n\n',
            '<details>\n<summary>展開 Mermaid 原始碼</summary>\n\n```mermaid\n',
            (out/(ident+".mmd")).read_text(encoding="utf-8"),'```\n\n</details>\n\n'])
    (ROOT/"技術流程圖.md").write_text("".join(parts).rstrip()+"\n",encoding="utf-8")
