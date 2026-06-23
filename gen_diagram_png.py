"""
Generates Assets/Docs/ClassDiagram.png using Pillow only (no extra deps).
Run:  python gen_diagram_png.py
"""
from __future__ import annotations
import math, os
from PIL import Image, ImageDraw, ImageFont

# ── Canvas ────────────────────────────────────────────────────────────────────
W, H = 1720, 870

# ── Palette ───────────────────────────────────────────────────────────────────
BG           = (10, 14, 31)
GRID_COL     = (20, 30, 55)

C_IFACE      = (100, 40, 200)    # purple  — interface
C_STATIC     = (160,  90,  10)   # amber   — static class
C_SINGLETON  = ( 13, 120,  90)   # teal    — singleton
C_MONO       = ( 15,  75, 130)   # navy    — MonoBehaviour
C_SO         = (120,  40,  15)   # brick   — ScriptableObject
C_UI         = ( 25,  58, 100)   # deep blue — UI

TEXT_MAIN    = (226, 232, 240)
TEXT_SUB     = (100, 116, 139)
TEXT_GROUP   = ( 80,  95, 120)

AR_INHERIT   = (167, 139, 250)   # purple
AR_CREATES   = ( 52, 211, 153)   # green
AR_USES      = ( 96, 165, 250)   # blue
AR_EVENTS    = (245, 158,  11)   # amber
AR_DAMAGES   = (248, 113, 113)   # red

def lighter(col, amt=55):
    return tuple(min(255, c + amt) for c in col)

def with_alpha(col, a):
    return col + (a,)

# ── Font loading ───────────────────────────────────────────────────────────────
FONT_PATHS = [
    r"C:\Windows\Fonts\consola.ttf",
    r"C:\Windows\Fonts\courbd.ttf",
    r"C:\Windows\Fonts\cour.ttf",
    r"C:\Windows\Fonts\arial.ttf",
]
def load_font(size, bold=False):
    for p in FONT_PATHS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()

FONT_NAME   = load_font(13, bold=True)
FONT_STEREO = load_font(10)
FONT_LABEL  = load_font(11)
FONT_TITLE  = load_font(18, bold=True)
FONT_LEGEND = load_font(10)
FONT_GROUP  = load_font(9)

# ── Class boxes ───────────────────────────────────────────────────────────────
# (id, display_name, stereotype, color, x, y, w, h)
BOXES = [
    ("IDamageable",        "IDamageable",         "«interface»",  C_IFACE,     750, 22, 155, 54),
    ("RunSession",         "RunSession",           "«static»",     C_STATIC,    28, 22, 138, 54),
    ("GameEvents",         "GameEvents",           "«static»",     C_STATIC,   180, 22, 138, 54),
    ("ScreenShake",        "ScreenShake",          "«singleton»",  C_SINGLETON,360, 22, 140, 54),

    ("BootLoader",         "BootLoader",           "MB",           C_MONO,      28,110, 138, 50),
    ("Milestone1Game",     "Milestone1Game",        "MB",           C_MONO,     180,110, 158, 50),
    ("BackgroundScroller", "BackgroundScroller",   "MB",           C_MONO,      28,178, 158, 50),

    ("GameManager",        "GameManager",          "«singleton»",  C_SINGLETON,360,110, 138, 50),
    ("ScoreManager",       "ScoreManager",         "«singleton»",  C_SINGLETON,512,110, 140, 50),
    ("AudioManager",       "AudioManager",         "«singleton»",  C_SINGLETON,666,110, 140, 50),
    ("VFXManager",         "VFXManager",           "«singleton»",  C_SINGLETON,820,110, 128, 50),
    ("PowerUpManager",     "PowerUpManager",       "«singleton»",  C_SINGLETON,962,110, 148, 50),
    ("WaveManager",        "WaveManager",          "MB",           C_MONO,    1124,110, 138, 50),

    ("PlayerController",   "PlayerController",     "MB",           C_MONO,      28,290, 148, 50),
    ("PlayerHealth",       "PlayerHealth",         "IDamageable",  C_MONO,      28,358, 148, 50),
    ("PlayerWeapon",       "PlayerWeapon",         "MB",           C_MONO,     192,358, 140, 50),

    ("Enemy",              "Enemy",                "IDamageable",  C_MONO,     962,290, 128, 50),
    ("EnemyShooter",       "EnemyShooter",         "MB",           C_MONO,    1106,290, 134, 50),
    ("BossController",     "BossController",       "IDamageable",  C_MONO,    1256,290, 150, 50),

    ("Projectile",         "Projectile",           "MB",           C_MONO,     400,290, 128, 50),
    ("PowerUp",            "PowerUp",              "MB",           C_MONO,     544,290, 128, 50),

    ("LevelData",          "LevelData",            "«SO»",         C_SO,       1430,110, 120, 50),
    ("WaveData",           "WaveData",             "«SO»",         C_SO,       1430,178, 120, 50),
    ("EnemyData",          "EnemyData",            "«SO»",         C_SO,       1430,246, 120, 50),
    ("BossData",           "BossData",             "«SO»",         C_SO,       1565,110, 120, 50),
    ("PowerUpData",        "PowerUpData",          "«SO»",         C_SO,       1565,178, 120, 50),

    ("UIManager",          "UIManager",            "MB",           C_UI,        28,480, 135, 50),
    ("MenuController",     "MenuController",       "MB",           C_UI,       178,480, 148, 50),
    ("MenuAnimator",       "MenuAnimator",         "MB",           C_UI,       340,480, 138, 50),
    ("EndScreenController","EndScreenCtrl",        "MB",           C_UI,       494,480, 158, 50),
    ("RuntimeScreenUI",    "RuntimeScreenUI",      "«static»",     C_STATIC,   668,480, 150, 50),
]

# ── Relationships ─────────────────────────────────────────────────────────────
# (from_id, to_id, label, color, dash)  dash: 0=solid 4=dashed 2=dotted
ARROWS = [
    ("PlayerHealth",     "IDamageable",      "implements",  AR_INHERIT,  2),
    ("Enemy",            "IDamageable",      "implements",  AR_INHERIT,  2),
    ("BossController",   "IDamageable",      "implements",  AR_INHERIT,  2),

    ("Milestone1Game",   "GameManager",      "creates",     AR_CREATES,  0),
    ("Milestone1Game",   "ScoreManager",     "creates",     AR_CREATES,  0),
    ("Milestone1Game",   "AudioManager",     "creates",     AR_CREATES,  0),
    ("Milestone1Game",   "VFXManager",       "creates",     AR_CREATES,  0),
    ("Milestone1Game",   "PowerUpManager",   "creates",     AR_CREATES,  0),
    ("Milestone1Game",   "WaveManager",      "creates",     AR_CREATES,  0),

    ("WaveManager",      "Enemy",            "spawns",      AR_CREATES,  0),
    ("WaveManager",      "BossController",   "spawns",      AR_CREATES,  0),
    ("WaveManager",      "LevelData",        "reads",       AR_USES,     4),

    ("PowerUpManager",   "PowerUp",          "spawns",      AR_CREATES,  0),

    ("ScoreManager",     "GameEvents",       "fires",       AR_EVENTS,   4),
    ("PlayerHealth",     "GameEvents",       "fires",       AR_EVENTS,   4),
    ("GameManager",      "GameEvents",       "fires",       AR_EVENTS,   4),
    ("UIManager",        "GameEvents",       "listens",     AR_EVENTS,   4),

    ("GameManager",      "RunSession",       "writes",      AR_USES,     0),
    ("ScoreManager",     "RunSession",       "writes",      AR_USES,     0),
    ("MenuController",   "RunSession",       "reads",       AR_USES,     4),

    ("Projectile",       "PlayerHealth",     "damages",     AR_DAMAGES,  4),
    ("Projectile",       "Enemy",            "damages",     AR_DAMAGES,  4),
    ("Projectile",       "BossController",   "damages",     AR_DAMAGES,  4),

    ("PowerUp",          "PlayerHealth",     "heals",       AR_CREATES,  4),
    ("PowerUp",          "PlayerWeapon",     "upgrades",    AR_CREATES,  4),
    ("PowerUp",          "PowerUpData",      "uses",        AR_USES,     0),

    ("LevelData",        "WaveData",         "contains",    AR_USES,     0),
    ("LevelData",        "BossController",   "refs",        AR_USES,     0),
    ("WaveData",         "EnemyData",        "refs",        AR_USES,     0),
    ("Enemy",            "EnemyData",        "uses",        AR_USES,     0),
    ("BossController",   "BossData",         "uses",        AR_USES,     0),

    ("PlayerWeapon",     "Projectile",       "fires",       AR_DAMAGES,  0),
    ("EnemyShooter",     "Projectile",       "fires",       AR_DAMAGES,  0),
    ("BossController",   "Projectile",       "fires",       AR_DAMAGES,  0),

    ("MenuController",   "RuntimeScreenUI",  "uses",        AR_USES,     4),
    ("EndScreenController","RuntimeScreenUI","uses",        AR_USES,     4),
    ("GameManager",      "PlayerHealth",     "respawns",    AR_CREATES,  4),
]

# ── Helpers ───────────────────────────────────────────────────────────────────
def box_dict():
    return {b[0]: b for b in BOXES}

def center(b):
    _, _, _, _, x, y, w, h = b
    return x + w // 2, y + h // 2

def edge_pt(b, tx, ty):
    _, _, _, _, x, y, w, h = b
    cx, cy = x + w / 2, y + h / 2
    dx, dy = tx - cx, ty - cy
    if dx == 0 and dy == 0:
        return int(cx), int(cy)
    sx = (w / 2) / abs(dx) if dx else 1e9
    sy = (h / 2) / abs(dy) if dy else 1e9
    s = min(sx, sy)
    return int(cx + dx * s), int(cy + dy * s)

def draw_arrow(draw, x1, y1, x2, y2, color, dash=0, width=2):
    dx, dy = x2 - x1, y2 - y1
    dist = math.hypot(dx, dy) or 1
    # shorten tip
    x2s = int(x2 - dx / dist * 4)
    y2s = int(y2 - dy / dist * 4)

    if dash == 0:
        draw.line([(x1, y1), (x2s, y2s)], fill=color, width=width)
    else:
        seg, gap = dash, dash
        cx, cy = x1, y1
        total = math.hypot(x2s - x1, y2s - y1)
        pos = 0
        drawing = True
        while pos < total:
            npos = min(pos + (seg if drawing else gap), total)
            nx = x1 + dx / dist * npos
            ny = y1 + dy / dist * npos
            if drawing:
                draw.line([(int(cx), int(cy)), (int(nx), int(ny))], fill=color, width=width)
            cx, cy = nx, ny
            pos = npos
            drawing = not drawing

    # arrowhead
    angle = math.atan2(y2 - y1, x2 - x1)
    size = 10
    spread = 0.42
    pts = [
        (x2, y2),
        (int(x2 - size * math.cos(angle - spread)),
         int(y2 - size * math.sin(angle - spread))),
        (int(x2 - size * math.cos(angle + spread)),
         int(y2 - size * math.sin(angle + spread))),
    ]
    draw.polygon(pts, fill=color)

def text_center(draw, x, y, text, font, color):
    bb = draw.textbbox((0, 0), text, font=font)
    tw = bb[2] - bb[0]
    th = bb[3] - bb[1]
    draw.text((x - tw // 2, y - th // 2), text, font=font, fill=color)

# ── Main render ───────────────────────────────────────────────────────────────
def render():
    img = Image.new("RGB", (W, H), BG)
    draw = ImageDraw.Draw(img)

    # Grid
    for gx in range(0, W, 60):
        draw.line([(gx, 0), (gx, H)], fill=GRID_COL, width=1)
    for gy in range(0, H, 60):
        draw.line([(0, gy), (W, gy)], fill=GRID_COL, width=1)

    bd = box_dict()

    # ── Arrows (behind boxes) ──────────────────────────────────────────────
    for fr, to, label, color, dash in ARROWS:
        if fr not in bd or to not in bd:
            continue
        fb, tb = bd[fr], bd[to]
        fcx, fcy = center(fb)
        tcx, tcy = center(tb)
        x1, y1 = edge_pt(fb, tcx, tcy)
        x2, y2 = edge_pt(tb, fcx, fcy)
        draw_arrow(draw, x1, y1, x2, y2, color, dash=dash, width=2)
        # label at midpoint
        mx, my = (x1 + x2) // 2, (y1 + y2) // 2
        draw.text((mx + 2, my - 9), label, font=FONT_STEREO, fill=color)

    # ── Boxes ─────────────────────────────────────────────────────────────
    for bid, name, stereo, col, bx, by, bw, bh in BOXES:
        border = lighter(col, 55)
        is_impl = stereo == "IDamageable"
        disp = "«IDamageable»" if is_impl else stereo

        # Shadow
        draw.rounded_rectangle([bx+3, by+3, bx+bw+3, by+bh+3], radius=7, fill=(0, 0, 0))
        # Box fill
        draw.rounded_rectangle([bx, by, bx+bw, by+bh], radius=7, fill=col, outline=border, width=2)
        # Header bar (slightly lighter top strip)
        draw.rounded_rectangle([bx+1, by+1, bx+bw-1, by+20], radius=6, fill=lighter(col, 20))

        # Stereotype text
        text_center(draw, bx + bw // 2, by + 11, disp, FONT_STEREO, TEXT_SUB)
        # Class name
        text_center(draw, bx + bw // 2, by + 34, name, FONT_NAME, TEXT_MAIN)

    # ── Group labels ───────────────────────────────────────────────────────
    groups = [
        (20,   85, 316, "── BOOTSTRAP & CORE ──"),
        (20,   265, 340, "── PLAYER ──"),
        (390,  265, 300, "── SHARED OBJECTS ──"),
        (950,  265, 465, "── ENEMIES ──"),
        (1418, 85,  280, "── DATA (ScriptableObjects) ──"),
        (20,   455, 820, "── USER INTERFACE ──"),
    ]
    for gx, gy, gw, glbl in groups:
        draw.rectangle([gx, gy, gx + gw, gy + 14], fill=(18, 28, 50))
        draw.text((gx + 6, gy + 2), glbl, font=FONT_GROUP, fill=TEXT_GROUP)

    # ── Legend (boxes) ─────────────────────────────────────────────────────
    lx, ly = 28, 605
    draw.rectangle([lx - 6, ly - 6, lx + 790, ly + 24], fill=(17, 24, 39), outline=(30, 45, 70))
    legend_types = [
        (C_IFACE,    "Interface"),
        (C_STATIC,   "Static class"),
        (C_SINGLETON,"Singleton (MB)"),
        (C_MONO,     "MonoBehaviour"),
        (C_SO,       "ScriptableObject"),
        (C_UI,       "UI component"),
    ]
    for i, (lc, ll) in enumerate(legend_types):
        lxx = lx + i * 132
        draw.rounded_rectangle([lxx, ly + 2, lxx + 18, ly + 14], radius=3,
                                fill=lc, outline=lighter(lc, 50))
        draw.text((lxx + 22, ly + 2), ll, font=FONT_LEGEND, fill=TEXT_SUB)

    # ── Legend (arrows) ────────────────────────────────────────────────────
    lx2, ly2 = 28, 645
    draw.rectangle([lx2 - 6, ly2 - 6, lx2 + 790, ly2 + 24], fill=(17, 24, 39), outline=(30, 45, 70))
    legend_arrows = [
        (AR_INHERIT, "implements",    2),
        (AR_CREATES, "creates/spawns",0),
        (AR_USES,    "uses/reads",    4),
        (AR_EVENTS,  "events",        4),
        (AR_DAMAGES, "damages",       4),
    ]
    for i, (lc, ll, ld) in enumerate(legend_arrows):
        lxx = lx2 + i * 158
        draw_arrow(draw, lxx, ly2 + 8, lxx + 30, ly2 + 8, lc, dash=ld, width=2)
        draw.text((lxx + 34, ly2 + 2), ll, font=FONT_LEGEND, fill=TEXT_SUB)

    # ── Title ──────────────────────────────────────────────────────────────
    draw.text((28, 695), "RETRO SPACE SHOOTER — CLASS DIAGRAM", font=FONT_TITLE, fill=TEXT_MAIN)

    return img

# ── Entry point ───────────────────────────────────────────────────────────────
if __name__ == "__main__":
    out = "Assets/Docs/ClassDiagram.png"
    img = render()
    img.save(out, "PNG", optimize=False)
    print(f"[OK] {out}  ({img.width}×{img.height})")
