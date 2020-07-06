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

do load %utils.r3
do load %link-builder.r3
do load %gopher-utils.r3


arg-block:  system/options/args

either not none? arg-block [
    in-path:  to-rebol-file (to-string debase/base arg-block/1 64)
    out-path:  to-rebol-file (to-string debase/base arg-block/2 64)
    
    uri:  to-string debase/base arg-block/3 64

] [
    
    folder: {C:\Users\lukee\Desktop\programming\projects\GemiNaut\GemiNaut\GmiConverters\TestContent\}

    in-path: to-rebol-file join folder {gophertext.txt}
    out-path: to-rebol-file join folder {gophertext.gmi}
    uri: "gopher://test/foomd/hello-wrld"
    
;    in-path: to-rebol-file {C:\Users\lukee\AppData\Local\Temp\geminaut_fkih3dqz.gj1\0e2734cc9c572d7221c9d09a6f711063.txt}
     
]


extension: last parse/all uri "."

lines: read/lines  in-path

out: copy []

;dont try to auto link in these text files (detected by URL file extension) as it will likely be wrong
exclude-extensions: ["htm" "html" "md" "gmi" "gemini"]

append out join "# " gopher-uri-to-title uri true  ;get a nice title  trimming off a trailing extension if necessary

foreach line lines [
        
    trimmed-line: trim copy line
    first-word: first parse line none
    ;probe fields
    
    whitespace: [" " | "^-"]
    known-scheme: ["gopher://" | "gemini://" | "http://" | "https://" ]
    digit: charset [#"0" - #"9"]
    
    either none? find exclude-extensions extension [
    
        ;---probably these could be simplified further...
        result: any [
           
           ;lines of form [label] url, common form used by phloggers, also matches [3] url forms
           if (parse trimmed-line [ "[" copy label thru "]" thru whitespace to known-scheme copy url to end]) [
                either  (1 = length? (parse/all url " ")) [
                    rejoin [
                        "=> " url " " trimmed-line
                    ] 
                ] [
                    gopher-escape line
                ]
           ]

           ;lines of form [label]url, common form used by phloggers, also matches [3]url forms
           if (parse trimmed-line [ "[" copy label to "]"  thru "]"  to known-scheme copy url to end]) [
                either  (1 = length? (parse/all url " ")) [
                    rejoin [
                        "=> " url " [" label "] " url
                    ]
                ] [
                    gopher-escape line                
                ]
           ]

        ;lines of form 1: url or 1. url
           if (parse trimmed-line [  digit ["." | ":"  ]  thru whitespace to known-scheme copy url to end]) [
                
                parse trimmed-line [to digit copy label to ["." | ":"] to end ]
                 rejoin [
                    "=> " url " [" label "] " url       ;normalise in square brackets, e.g. 3. url -> [3] url
                ]
           ]
           
        ;lines of form nn: url or 1nn. url
           if (parse trimmed-line [   digit  digit ["." | ":"  ]  thru whitespace to known-scheme copy url to end]) [
                
                parse trimmed-line [to digit copy label to ["." | ":"] to end ]
                 rejoin [
                    "=> " url " [" label "] " url       ;normalise in square brackets, e.g. 3. url -> [3] url
                ]
           ]           

           if (parse trimmed-line [ digit  digit  digit ["." | ":"  ]  thru whitespace to known-scheme copy url to end]) [
                
                parse trimmed-line [to digit copy label to ["." | ":"] to end ]
                 rejoin [
                    "=> " url " [" label "] " url       ;normalise in square brackets, e.g. 3. url -> [3] url
                ]
           ]           

           ;pseudo bullets e.g. underneath gopher://gopher.floodgap.com/1/feeds/wikinews
           if (parse trimmed-line [  ["-"  | "*" ]  thru whitespace to known-scheme copy url to end]) [
                rejoin [
                    "=> " url " " url
                ]
           ]


        ;lines of foo URL: url, e.g. underneath gopher://gopher.floodgap.com/1/feeds/wikinews
           if (parse trimmed-line [ thru " URL:"  thru whitespace to known-scheme copy url to end]) [
               rejoin [
                    "=> " url " " trimmed-line
                ]
           ]
           
               ;lines of Original Article: url, e.g. underneath gopher://gopherpedia.com
           if (parse trimmed-line [ "Original Article:"  thru whitespace to known-scheme copy url to end]) [
                rejoin [
                    "=> " url " " trimmed-line
                ]
           ]
           
          ;lines of form: url, quite common
           if (parse trimmed-line [known-scheme copy url to end])  and (1 = length? (parse/all trimmed-line " ") )[
                rejoin [
                    "=> " trimmed-line " " trimmed-line
                ]       
           ]
           
          ;lines of the form "<url>", e.g. gopher://gopher.floodgap.com/0/feeds/tidbits/2008/Aug/25/5 and similar
          if (parse trimmed-line ["<" to known-scheme copy url to ">" to end])  and (1 = length? (parse/all trimmed-line " ") )[
               either (trimmed-line = rejoin ["<" url ">" ]) [
                   rejoin [
                        "=> " url " " url
                    ]       
                ] [
                    gopher-escape line
                ]
           ]



           ;could markdown headers through like this, but gopher does not have this convention really
           ;so currently disabled, as it could spuriously pick up other lines
           ;if (first-word = "#") [line]
           ;if (first-word = "##") [line]
           ;if (first-word = "###") [line]
           
           ;otherwise...
           gopher-escape line    ;effectively escapes the content from further processing, is removed by GmiToHTML
           
        ]
    ] [
           result: gopher-escape line    ;effectively escapes the content from further processing, is removed by GmiToHTML
    ]

    append out result

]
out-string: copy ""

foreach line out [
    append out-string join  line  newline
]

;save to final location
write  out-path  out-string

