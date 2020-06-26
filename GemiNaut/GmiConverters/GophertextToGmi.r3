REBOL [

    title: "Gophertext to GMI converter"
    
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

do load %utils.r3
do load %link-builder.r3
do load %gopher-utils.r3


arg-block:  system/options/args

if (error? try [
    in-path:  to-rebol-file (to-string debase/base arg-block/1 64)
    out-path:  to-rebol-file (to-string debase/base arg-block/2 64)

    ]) [
    
    folder: {C:\Users\lukee\Desktop\programming\projects\GemiNaut\GemiNaut\GmiConverters\TestContent\}

    in-path: to-rebol-file join folder {gophertext.txt}
    out-path: to-rebol-file join folder {gophertext.gmi}
     
]


lines: read/lines in-path

out: copy []


foreach line lines [
        
    trimmed-line: trim copy line
    first-word: first parse line none
    ;probe fields
    
    whitespace: [" " | "^-"]
    known-scheme: ["gopher://" | "gemini://" | "http://" | "https://" ]
    
    result: any [
       
       if (parse trimmed-line [ "[" copy label thru "]" thru whitespace to known-scheme copy url to end]) [
           rejoin [
                "=> " url " " trimmed-line
            ]
       ]
       

       if (parse trimmed-line [known-scheme copy url to end])  and (1 = length? (parse/all trimmed-line " ") )[
            rejoin [
                "=> " trimmed-line " " trimmed-line
            ]       
       ]


       ;could markdown headers through like this
       ;if (first-word = "#") [line]
       ;if (first-word = "##") [line]
       ;if (first-word = "##") [line]
       
       ;otherwise...
       gopher-escape line    ;effectively escapes the content from further processing, is removed by GmiToHTML->utils.r3
       
    ]

    append out result

]
out-string: copy ""

foreach line out [
    append out-string join  line  newline
]

;save to final location
write out-path  out-string
