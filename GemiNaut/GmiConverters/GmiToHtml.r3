REBOL [

    title: "GMI to html converter"
    
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
    uri:   (to-string debase/base arg-block/3 64)
    theme:   (to-string debase/base arg-block/4 64)

        ;uri: "gemini://gemini.circumlunar.space/"

    ]) [
    
    folder: {C:\Users\lukee\Desktop\programming\projects\GemiNaut\GemiNaut\GmiConverters\TestContent\}

    in-path: to-rebol-file join folder {test1.gmi}
    out-path: to-rebol-file join folder {test1.htm}
    uri: "gemini://gemini.circumlunar.space/users/foo?.gz"
    theme: "Fabric"
    
     ;in-path: to-rebol-file {C:\Users\lukee\Desktop\geminaut\b8667ef276b02664b2c1980b5a5bcbe2.gmi}
     ;in-path: to-rebol-file {C:\Users\lukee\Desktop\geminaut\9fdfcb2ef4244d6821091d62e3a0e06a.gmi}
   
    in-path: to-rebol-file {C:/Users/lukee/AppData/Local/Temp/geminaut_t1qyj52a.43x/b25e5c0c2d5f3f2e271f16a7c17429e7.txt}
     
]


uri-object:   decode-url uri

;---determine an theming identity hash for this site based on its url, to be used for identikon
;---and textured "fabric" style background
site-id: get-site-id uri-object
uri-md5: lowercase copy/part at (mold checksum/method (to-binary site-id) 'md5) 3 32

lines: read/lines in-path

out: copy []

in-block: false



last-element: 'empty

first-heading: copy ""
first-text-line: copy ""

table-of-contents: copy []
table-of-contents-string: copy ""
heading-count: 0

make-toc-entry: funct [heading-text heading-level id] [
         rejoin  [
            {<div class="toc} heading-level {"><a class=toc title="Item on this page" href="#} id {">} 
            (markup-escape heading-text) 
            {</a></div>} 
            newline
        ]
]

insert-missing-preceding-line: funct [] [
    
    if  0 < length? out [
        ;ensure certain items are preceded by a blank line
        ;apart from first line
        if ("<div>&nbsp;</div>" <> last out) [
            append out  "<div>&nbsp;</div>" 
        ]
    ]
]



add-to-toc: func [display level] [
         if first-heading = "" [first-heading: display]
         heading-count:  heading-count + 1
         append table-of-contents make-toc-entry display level (join "id" heading-count )
]

process-heading: func [line level] [
         insert-missing-preceding-line
         display: trim take-from  line (level + 1)
         add-to-toc (copy display) level
         last-element: 'heading
         rejoin [{<h} level { id="} (join "id" heading-count) {">} (markup-escape display) {</h} level {>}]
]    


foreach line lines [

    
    ;--start or end preformatted block. Use any label as the tooltip as
    ;-- a hint to the user -e.g. ```python
    if ((take-left line 3) = "```") [
        
        pre-label: trim take-from line 4
        if pre-label = "" [pre-label:  "preformatted text"]
       in-block: not in-block
        last-element: 'preformat
        
        append out either in-block [
             rejoin [{<pre class=inline title="} pre-label {">} ]
        ] [
            "</pre>"
        ]
    ]
    

        
    
    
    either not  in-block [
    
        append/only out any [
            
            
            ;--handle headings, do most specific first so not to match later ones
            if  ("###" = take-left line 3) [
                process-heading line 3
            ]
            if  ("##" = take-left line 2) [
                process-heading line 2
            ]
            if  ("#" =  take-left line 1) [
                process-heading line 1
            ]
        


            ;---handle *  bullets, note that bullet asterisks must have a space after
            if ("* " = take-left line 2)  [
                if last-element <> 'bullet [ insert-missing-preceding-line]
                display: trim take-from line 3
                 last-element: 'bullet
                 rejoin ["<div class=bullet>&bull;&nbsp;" (markup-escape display) "</div>"]
            ]

            ;---handle quotes
            if (">" = take-left line 1)  [
                if last-element <> 'quote [ insert-missing-preceding-line]
                display: trim take-from line 2
                 last-element: 'quote
                 rejoin ["<div class=blockquote>" (markup-escape display) "</div>"]
            ]


            ;---handle links
            if ("=>" = take-left line 2) [

                link-content: trim take-from line 3
                
                ws:  [" " | tab]
                
                either (parse link-content [copy link-part to ws thru ws copy display-part to end]) [
                    link-content: trim link-content
                    display-part: trim display-part                
                ] [
                    link-part: trim link-content
                    display-part: trim link-content
                ]
                                
                if not find [bullet link] last-element [ insert-missing-preceding-line]
                last-element: 'link
                
                if unset? display-part [
                    display-part: link-part
                ]
                
               link: build-link uri-object link-part

                either (take-left link 9) = "gemini://" [
                    class: "gemini"
                    link-gliph: "&rArr;"        ;---fat arrow like =>
                    link-class: "gemini-link"
                    
                    ;---try to guess from the url if this is a link to a binary file (not implemented at present)
                    ;---and warn the user in the tooltip. we cannot know for certain as we cannot do any kind of HEAD
                    ;---request in gemini. Here are the most common binary file types we might expect.
                    binary-extensions: [
                        "tar" "gz" "zip" "exe" "7z" "tar"
                    ]
                    
                    image-extensions: [
                        "png" "gif" "jpg" "jpeg" "svg"  "bmp"                
                    ]
                    
                    audio-extensions: [
                        "mp3" "wav" "ogg"  "midi" "flac"                        
                    ]
                    
                    document-extensions: [
                       "htm" "html" "pdf" "ps" "odf" "epub" "mobi"
                        "xls" "xlsx" "ppt" "pptx" "doc" "docx" 
                    ]
                    
                    video-extensions: [
                        "wmv" "mp4" "mov" "swf"
                    ]
                    
                    display-extensions: [
                        ;for info put md txt in here as we dont want them externally opened, we will render them
                        ;but otherwise this colleciton is not used, just for info
                        "txt" "md" "gmi" "gemini"
                    ]
                    
                    
                    ;--we launch external all known extensions but not display-extensions
                    launch-external-extensions:  join binary-extensions join image-extensions join audio-extensions join document-extensions video-extensions
                    
                    url-split: parse/all link-part "."
                    link-extension: (lowercase last copy url-split)
                    display-extension: (lowercase last parse display-part ".")
                    
                    ;see if it is a plain path (not a dynamic page) and a known extension
                    either (not none? find launch-external-extensions last url-split)  and (none? find link "?") [
                                
                        final-link-object: probe decode-url to-url link
                        
                        show-extension: copy ""
                        
                        any [
                        
                            ;images -> losange flower
                            if find image-extensions last url-split [decorator-glyph: "&#128160;"  ]

                            ;audio-> headphones
                            if find audio-extensions last url-split [ decorator-glyph: "&#127911;"  ]
                            
                            ;video->film frames
                            if find video-extensions last url-split [decorator-glyph: "&#127902;" ]
                            
                            ;document-> page facing up
                            if find document-extensions last url-split [decorator-glyph: "&#128196;"]
                            
                           ;default - a generic cabinet
                           if true [
                            decorator-glyph: "&#128452;"            ;document
                            show-extension:  rejoin [" [" link-extension "]"]
                            ]
                            
                        ]

                        
                        tooltip:  rejoin [
                                        decorator-glyph
                                        { opens link to expected } 
                                        (uppercase copy link-extension)
                                        { file in system web browser:}
                                        { } (last parse/all link   "/")
                                    ]
                                    
                        ;--decorate links to binary files with a unicode document glyph (&#128462;), 
                        ;--their expected file type and a tooltip to explain these are opened in system browser
                        display-html: rejoin [
                                {<span class=decorator-gliph }
                                    { title="} tooltip {">} decorator-glyph {</span>}
                                    {<span style=text-decoration:none>&nbsp;</span>}
                                    {<span title="} tooltip {"}
                                {>}
                                (markup-escape display-part)
                                
                                ;show extension if not visible 
                                either (link-extension <> display-extension) [
                                   show-extension
                                ] [""]
                                                                
                                "</span>"]
                                
                            ;use an http/html proxy for these binary files which will open
                            ;in users standard browser. In future we might handle them directly.
                            link: rejoin [
                                "https://portal.mozz.us/gemini"
                                "/"
                                final-link-object/host
                                final-link-object/path
                                "?raw=1"
                            ]                        

                    ] [
                        display-html: markup-escape display-part
                    ]
                
                ] [
                    display-html: markup-escape display-part
                    class: "other"
                    link-gliph: "&#8669;"   ;---squiggle arrow for non gemini targets to aid recognition
                    link-class: "other-link"

                ]
                
               rejoin [
                    {<div class="} link-class {">}
                {<span class="link-gliph">} link-gliph {</span>} 
                    {&nbsp;<a } 
                        { href="} link {"}
                        { title="} link {"}
                        { class="} class {"}
                        {>}   (trim display-html) "</a></div>"
                ]
            ]
            
            if ((take-left line 3) = "```") [""]

            ;--default - not a spaced item
            if true [                
                
                
               either line = "" [
                       last-element: 'empty
                       display-html: "<div>&nbsp;</div>"
                ] [
                    if not find [line empty] last-element  [
                        insert-missing-preceding-line
                        last-element: 'line
                    ]    
                    
                    if first-text-line = "" [first-text-line: join (take-left line 60) "..."]

                    display-html:  rejoin ["<div>" markup-escape line "</div>"]
                ]
                
               
                :display-html
                
            ]
        ]
    ] [
            if ((take-left line 3) <> "```") [
                last-element: 'preformat
                append/only out markup-escape line
            ]
    ]

]

out-string: copy ""

foreach line out [
    append out-string join  line  newline
]

;---pad with some empty lines, if less than 30 otherwise looks strange on short content
for n 1 (30 - length? out) 1 [
    append out-string join "<div>&nbsp;</div>" newline
]

page-title: any [
    if first-heading <> "" [first-heading]
    if first-text-line <> "" [first-text-line]
    uri
]

if (page-title <> uri)  and (page-title <> "") [
    page-title: rejoin [
        site-id
        " - "
        page-title
    ]
]

;---heuristic of only showing TOC if more than one
;--quite often a simple page has a single heading at the top
if  (1 < length? table-of-contents ) [
    table-of-contents-string: rejoin [
        {<div id=toc-container>} newline
        newline
        {<div id=toc-label>Contents</div>}  newline
        (rejoin table-of-contents) newline
        {</div>} newline
        newline
    ]
]
    
;--save the content to a HTML file
;--theme html should be UTF-8 charset, which is the standard format.
theme-html: read/string to-file rejoin [theme ".htm"]
theme-css: read/string to-file rejoin [theme ".css"]


;populate the theme
replace/all theme-html "{{title}}" page-title
replace/all theme-html "{{theme-css}}" theme-css
replace/all theme-html "{{table-of-contents}}" table-of-contents-string
replace/all theme-html "{{site-id}}" site-id
replace/all theme-html "{{site-id-md5}}" uri-md5
replace/all theme-html "{{site-id-md5-reversed}}" (reverse copy uri-md5)
replace/all theme-html "{{content}}" out-string

;save to final location
write out-path  theme-html
