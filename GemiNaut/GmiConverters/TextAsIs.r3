REBOL [

    title: "Gophertext to GMI converter"
    
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


arg-block:  system/options/args

either not none? arg-block [
    in-path:  to-rebol-file (to-string debase/base arg-block/1 64)
    out-path:  to-rebol-file (to-string debase/base arg-block/2 64)
    
] [
    
    folder: {C:\Users\lukee\Desktop\programming\projects\GemiNaut\GemiNaut\GmiConverters\TestContent\}

    in-path: to-rebol-file join folder {gophertext.txt}
    out-path: to-rebol-file join folder {gophertext.gmi}
    uri: "gopher://test/foomd/hello-wrld"
    
;    in-path: to-rebol-file {C:\Users\lukee\AppData\Local\Temp\geminaut_fkih3dqz.gj1\0e2734cc9c572d7221c9d09a6f711063.txt}
     
]


do load %utils.r3

lines: read/lines  in-path

out: copy []

append out join "```" newline
foreach line lines [

    ;escape 3 back ticks, since the content goes into a preformatted area
    either "```" = take-left line 3 [
         append out  join " " line      ;prepend with space
    ] [
         append out line
    ]
]

append out join "```" newline

out-string: copy ""

foreach line out [
    append out-string join  line  newline
]

;save to final location
write  out-path  out-string

