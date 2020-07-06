REBOL [
    title: "utilities module"
]

;===================================================
;    GemiNaut, a friendly browser for Gemini space on Windows
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


take-from: funct [text start] [
    substring text start (length? text)
]

take-left: funct [text length] [
    substring text 1 length
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


markup-escape: funct [data-in] [

    data: copy data-in
    
    ;mostly gopher pages seem to hard wrap at 70, but occasionally 100. Some just are 
    ;very long (a single paragraph-line), which need soft wrapping
    ;so for now I've decided to turn on soft-wrap for those lines longer than 85
    can-wrap: 85 < length? data
    
    replace/all data "&" "&amp;"
    replace/all data "<" "&lt;"
    replace/all data ">" "&gt;"
    replace/all data {"} "&quot;"
    replace/all data {'} "&apos;"
    
    
    if page-scheme = 'gopher [
                
        either can-wrap [
            ;--we want to keep the white spaces, but wrap at the window
            ;--so whitespace is signigicant, but we permit it to wrap between words if necessary
            ;--e.g. some gopher texts are not hard wrapped, but we still want significant whitespace
            if  " " = take-left data 1 [data: join "&nbsp;" take-from data 2]
            replace/all data "  " " &nbsp;"
            replace/all data "^-" "&nbsp;&nbsp;"    ;tabs
        ] [
           ;---generally it looks nicer if shorter content never wraps and this is our best attempt to prevent that
            replace/all data " " "&nbsp;"       ;non breaking space
            replace/all data "^-" "&nbsp;&nbsp;"    ;tabs
            ;replace/all data "-" "&#8209;"      ;non breaking hyphen - would be nice, but it is not same width as other characters in some fonts :-/
        ]
    ]
    
    :data
]

;probe markup-escape "ab>cd&f'oobar<"