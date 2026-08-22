# PEX integration fixture

`skyrim-se-3.2-unicode.pex.hex` is the original synthetic Skyrim Special Edition Papyrus 3.2 fixture published by
Modding-Forge/PexReader in release `v1.0.1.5`. It contains no Bethesda game assets or third-party mod content and is
distributed under the GNU Lesser General Public License version 3 used by both repositories.

The whitespace-separated hexadecimal representation keeps every byte reviewable. Tests materialize it only inside a
unique temporary directory and derive malformed inputs by truncating a temporary copy. The fixture covers a header,
UTF-8 strings, debug information, a user flag, variables, properties, a state, a function, and instruction arguments.
