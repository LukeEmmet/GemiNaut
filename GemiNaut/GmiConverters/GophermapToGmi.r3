REBOL [

    title: "Gophermap to GMI converter"
    
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


arg-block:  system/options/args

if (error? try [
    in-path:  to-rebol-file (to-string debase/base arg-block/1 64)
    out-path:  to-rebol-file (to-string debase/base arg-block/2 64)

    ]) [
    
    folder: {C:\Users\lukee\Desktop\programming\projects\GemiNaut\GemiNaut\GmiConverters\TestContent\}

    in-path: to-rebol-file join folder {gophermap.txt}
    out-path: to-rebol-file join folder {gophermap.gmi}
     
]


lines: read/lines in-path

out: copy []

extract-url: funct [gopher-field] [
    
    any [
        if ("/URL:" = take-left gopher-field 5) [take-from gopher-field 6]
        if ("URL:" = take-left gopher-field 4) [take-from gopher-field 5]
        gopher-field
    ]
]

foreach line lines [
    
    ;print ""
    selector:   take-left line 1
    rest: take-from line 2
    fields: parse/all rest tab 
    ;split on tabs
    
    path: copy fields/2
    replace/all path " " "%20"      ;v simplistic escaping spaces only
    
    ;probe fields
    result: any [
        ;--use == to be case sensitive test
       if (selector == "i") [fields/1]
        if (selector == "h") [
            rejoin [
                "=> " (extract-url fields/2) 
                " "
                fields/1
            ]
        ]
       
       
        rejoin [
            "=> gopher://" 
            fields/3
            (either (fields/4 = "70") [""] [join ":" fields/4])
            "/" 
            selector
            path

            " "
            fields/1 
        ]
    ]

    append out result

]
out-string: copy ""

foreach line out [
    append out-string join  line  newline
]

;save to final location
write out-path  out-string
