import sys
from pathlib import Path
DIR = Path(__file__).parent
sys.path.append(
    str(
        (
            DIR / "../common"
        ).resolve()
    )
)
