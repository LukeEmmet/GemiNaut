REBOL [
    title: "utilities module"
]

;===================================================
;    GemiNaut, a friendly browser for Gemini space on Windows
;    (and for related plucked instruments).
;
;    Copyright (C) 2020, Luke Emmet 
;
;    Email: luke [dot] emmet [at] gmail [dot] com
;
;    This program is free software: you can redistribute it and/or modify
;    it under the terms of the GNU General Public License as published by
;    the Free Software Foundation, either version 3 of the License, or
;    (at your option) any later version.
;
;    This program is distributed in the hope that it will be useful,
;    but WITHOUT ANY WARRANTY; without even the implied warranty of
;    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
;    GNU General Public License for more details.
;
;    You should have received a copy of the GNU General Public License
;    along with this program.  If not, see <https://www.gnu.org/licenses/>.
;===================================================

substring: func [string [string!] offset [integer!] length [integer!]] [
    copy/part at string offset length
]


;---string join ["a" "b"] -> "a/b" (inverse of split)
;---from https://stackoverflow.com/questions/46509781/inverse-of-split-function-join-a-string-using-a-delimeter/46511435
block-join: func [blk[block!] delim [string!]][
    outstr: copy ""
    repeat i ((length? blk) - 1)[
        append outstr blk/1
        append outstr delim
        blk: next blk ]
    append outstr blk 
]

;---from https://stackoverflow.com/questions/18231434/in-a-series-what-is-the-best-way-of-removing-the-last-element
remove-last: func [
    "Removes value(s) from tail of a series."
    series [series! port! bitset! none!]
    /part range [number!] "Removes to a given length."
] [
    either part [
        clear skip tail series negate range
    ] [
        remove back tail series
    ]
]

;---not sure yet why this doesnt work, produces strange output
;--- in final string.
;markup-escape: func [input-string] [
;   out: copy ""
;   foreach char copy input-string [
;        char-s: to-string char
;        append out any [
;        if char-s = ">" ["&gt;"]
;            if char-s = "<" ["&lt;"]
;            if char-s  = "&" ["&amp;"]
;            if char-s  = {"} ["&quot;"]
;            if char-s = "'" ["&apos;"]
;            
;            ;default
;            if true [char-s]
;        ]
;    ]
;    
;    to-string out
;]


markup-escape: func [data] [
    replace/all data "&" "&amp;"
    replace/all data "<" "&lt;"
    replace/all data ">" "&gt;"
    replace/all data {"} "&quot;"
    replace/all data {'} "&apos;"
]

;probe markup-escape "ab>cd&f'oobar<"