REBOL [

    title: "Gophermap to GMI converter"
    
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

do load %utils.r3
do load %gopher-utils.r3
do load %link-builder.r3


arg-block:  system/options/args

either not none? arg-block [
    in-path:  to-rebol-file (to-string debase/base arg-block/1 64)
    out-path:  to-rebol-file (to-string debase/base arg-block/2 64)
    
    uri:  (to-string debase/base arg-block/3 64)
 ] [
    
    folder: {C:\Users\lukee\Desktop\programming\projects\GemiNaut\GemiNaut\GmiConverters\TestContent\}

    in-path: to-rebol-file join folder {gophermap.txt}
    out-path: to-rebol-file join folder {gophermap.gmi}
     uri: "gopher://foo/"
     in-path: to-rebol-file {C:\Users\lukee\AppData\Local\Temp\geminaut_wzol1ojq.b5f\test2.txt}
]


lines: read/lines in-path

out: copy []

;--dont render links to CSO or telnet resources
unsupported-selectors: "8+T2"
supported-link-selectors: "0174569gI;d"     ;text, gophermap, query and additionally these files (for download)



append out join "# " gopher-uri-to-title uri false  ;get a nice title without trimming any trailing extension since this is a map

foreach line lines [
    
    if line <> "." [
        ;print ""
        selector:   take-left  line 1
        rest: take-from line 2
        
        ;split on tabs
        fields: split rest #"^-"        ;--better than parse which also loses quotes
        
        path: copy fields/2
        replace/all path " " "%20"      ;v simplistic escaping spaces only
        
        ;probe fields
        result: any [
            ;--use == to be case sensitive test
           if (selector == "i") [
                gopher-escape fields/1       ;effectively escapes the result from further processing, removed by GmiToHTML
            ]      
            if (selector == "h") or (selector == "H") [
                rejoin [
                    "=> " (extract-url fields/2) 
                    " "
                    fields/1
                ]
            ]
            
            if (selector == "3") [
                ;an error
                rejoin [
                    "* ERROR "
                    fields/1
                    " "
                    fields/2
                ]
            ]
           
           if (find supported-link-selectors selector) [
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
           
            
            ;unknown selector or unsupported one, render as a bullet
              rejoin [
                "* [Unknown or unsupported gopher selector " selector ": "
                 fields/1
                " "
                fields/3
                "]"
            
            ]

        ]
        append out result 
    ]

]

out-string: copy ""

foreach line out [
    append out-string join  line  newline
]

;save to final location
write out-path  out-string
