REBOL [

    title: "GMI to html converter"
    
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
do load %wire-up.r3


arg-block:  system/options/args

either not none? arg-block [
    in-path:  to-rebol-file (to-string debase/base arg-block/1 64)
    out-path:  to-rebol-file (to-string debase/base arg-block/2 64)
    uri:   (to-string debase/base arg-block/3 64)
    theme:  to-rebol-file (to-string debase/base arg-block/4 64)
    identicon-image: (to-string debase/base arg-block/5 64)
    fabric-image: (to-string debase/base arg-block/6 64)
    image-id:   (to-string debase/base arg-block/7 64)
    site-id:   (to-string debase/base arg-block/8 64)
    show-web-header:   (to-string debase/base arg-block/9 64) = "true"


        ;uri: "gemini://gemini.circumlunar.space/"

  ] [

    ;================================
    ;---this branch of the conditional is just for testing only, not used in production
    ;---uncomment to test features 
    ;===============================
    ;folder: {C:\Users\lukee\Desktop\programming\projects\GemiNaut\GemiNaut\GmiConverters\TestContent\}

    ;in-path: to-rebol-file join folder {test1.gmi}
    ;out-path: to-rebol-file join folder {test1.htm}
    ;uri: "gemini://test/"
    ;identicon-image: "[example-identicon-placeholder"
    ;fabric-image: "[example-image-placeholder]"
    ;theme: %Themes/Plain
    ;site-id: "domain/foo"
    ;image-id: "imageid"
    ;show-web-header: false
    
     
]


uri-object:   decode-url  uri
page-scheme: (to-word uri-object/scheme)

uri-md5: image-id   ;lowercase copy/part at (mold checksum/method (to-binary site-id) 'md5) 3 32        ;deprecated but classic fabric theme needs it

uri-extension: last parse/all uri-object/path "."
lines: read/lines in-path

;structure to hold info for hotlinks extracted from the content using square bracket footnotes
hotlinks: context [
    expand-citations: off
    citations: copy []
    replacements: make map! []      ;returns none if entry is missing
]

hot-wire-links: does [
    (page-scheme = 'gemini and (not none? find ["html" "htm"] uri-extension)) or 
    (page-scheme = 'http) or
    (page-scheme = 'https) or
    (page-scheme = 'gopher)

]

if  hot-wire-links [
   ;---only do fancy citations on web content for now
   hotlinks/citations: get-citations lines uri-object
   hotlinks/expand-citations: on
]

out: copy []

in-block: false
in-text-area: false



last-element: 'empty

first-heading: copy ""
first-text-line: copy ""

table-of-contents: copy []
table-of-contents-string: copy ""
heading-count: 0
preformatted-count: 0

make-toc-entry: funct [heading-text heading-level id] [
        
        
        ;trim length to a sensible size
        max-length: 70 
        use-text: copy heading-text
        if max-length < length? use-text [use-text: join (take-left use-text max-length) "…"]
        
         use-text: (markup-escape use-text) 
         replace/all use-text "&nbsp;" " "   ;--bit of a hack to undo the work of markup escape on gopher...        e.g. TOC for  gopher://gemini.circumlunar.space/0/docs/faq.txt
         rejoin  [
            {<div class="toc} heading-level {"><a class=toc title="Item on this page" href="#} id {">} 
            use-text
            {</a></div>} 
            newline
        ]
]

is-empty-div: funct [line] [
    either ( parse line ["<div" thru ">" copy content to  "</div>" copy rest to end]) [
       (content = "&nbsp;") and (rest = "</div>")
    ] [
        false
    ]
]


insert-missing-preceding-line: funct [] [
    
    if  0 < length? out [
        ;ensure certain items are preceded by a blank line
        ;apart from first line
        if (not is-empty-div last out) [
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
         rejoin [{<h} level { id="} (join "id" heading-count) {">} (apply-citation-set (markup-escape display) hotlinks  false) {</h} level {>}]
]    


foreach line lines [

    
    ;--start or end preformatted block. Use any label as the tooltip as
    ;-- a hint to the user -e.g. ```python
    if ((take-left line 3) = "```") [
        
        pre-label: trim take-from line 4    ;will be empty if not provided
        in-block: not in-block
        last-element: 'preformat
        
        either in-text-area or (pre-label = "editable") [
             either in-block [
                 in-text-area: true
                 append out rejoin [{<div class=edit-container><textarea rows=18 title="} (markup-escape pre-label) {">} ]
            ] [
                 in-text-area: false
               append out "</textarea></div>"


            ] 
        ] [
            ;normal gemini
                if in-block [preformatted-count: preformatted-count + 1]

            append out either in-block [
                 rejoin [
                    {<div class=screenreader><a href="#end-preformatted}  preformatted-count {">Skip over } (either (0 < length? trim pre-label) [join "preformatted section: " markup-escape pre-label] ["unlabelled preformatted section"]) {</a></div>}
                    {<pre class=inline title="} (markup-escape pre-label) {">} 
                ]
            ] [
               rejoin [
                    "</pre>"
                    {<div class=screenreader><a name="end-preformatted}  preformatted-count {"></a></div>}
                ]
            ] 
        
        ]
    ]
    

    ;flag to determine whether any wired up links in lines are done monospace style
    monospace-style: in-block or (page-scheme = 'gopher)

    
    
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
                if not find [bullet link] last-element [ insert-missing-preceding-line]
                display: trim take-from line 3
                 last-element: 'bullet
                 rejoin [{<div class="bullet } page-scheme {">&bull;&nbsp;} (apply-citation-set (markup-escape display) hotlinks false) "</div>"]
            ]

            ;---handle quotes
            if (">" = take-left line 1)  [
                if last-element <> 'quote [ insert-missing-preceding-line]
                display: trim take-from line 2
                 last-element: 'quote
                 rejoin [{<div class=blockquote } page-scheme {">} (apply-citation-set( markup-escape display) hotlinks false) "&nbsp;</div>"]
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

                either (
                            ((take-left link 9) = "gemini://") or 
                            ((take-left link 9) = "gopher://") 
                        ) [
                    class: "gemini"
                    link-gliph: "&rArr;"        ;---fat arrow like =>
                    link-class: "gemini-link"
                    
                    ;---try to guess from the url if this is a link to a binary file (not implemented at present)
                    ;---and warn the user in the tooltip. we cannot know for certain as we cannot do any kind of HEAD
                    ;---request in gemini. Here are the most common binary file types we might expect.
                    binary-extensions: [
                        "tar" "gz" "zip" "exe" "7z" "tar" "rar"
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
                            if find image-extensions last url-split [  decorator-glyph: "&#128160;" ]

                            ;audio-> headphones
                            if find audio-extensions last url-split [ decorator-glyph: "&#127911;"  ]
                            
                            ;video->film frames
                            if find video-extensions last url-split [decorator-glyph: "&#127902;" ]
                            
                            ;document-> page facing up
                            if find document-extensions last url-split [decorator-glyph: "&#128196;"]
                            
                           ;default - a generic cabinet, and show the actual extension on the link if not visible
                           if true [
                            decorator-glyph: "&#128452;"            ;cabinet
                            show-extension:  rejoin [" [" link-extension "]"]
                            ]
                            
                        ]

                        tooltip:  rejoin [
                                        decorator-glyph
                                        { link to } 
                                        (uppercase copy link-extension)
                                        { file: } (last parse/all link   "/")
                                    ]
                        
                                    
                        ;--decorate links to binary files with the glyph and tooltip, and optionally the extension if not shown 
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
                        

                    ] [
                        display-html: markup-escape display-part
                    ]
                
                ] [
                    display-html: markup-escape display-part
                    
                    either "nimigem://" = take-left link 10 [
                        class: "nimigem"
                        link-gliph: "&#8203;"   ;---solid arrow, thick
                        link-class: "nimigem-link"
                    ] [
                        class: "other"
                        link-gliph: "&#8669;"   ;---squiggle arrow for non gemini targets to aid recognition
                        link-class: "other-link"
                    ]
                ]
                
               link-html: rejoin [
                    {<div class="} link-class " "  page-scheme {">}
                {<span class="link-gliph">} link-gliph {</span>} 
                    {&nbsp;<a } 
                        { href="} link {"}
                        { title="} link {"}
                        { class="} class {"}
                        {>}   (trim display-html) "</a></div>"
                ]
                
                keep: true
                
                if hotlinks/expand-citations [
                    link-candidate:  extract-citation line uri-object
                    if not none? link-candidate [
                        index: link-candidate/1
                        keep: not hotlinks/replacements/:index
                    ]
                ]
                either keep [link-html] [""]

            ]
            
            if ((take-left line 3) = "```") [""]

            ;--default - not a spaced item
            if true [                
                
                if page-scheme = 'gopher [
                    line: gopher-unescape line
                ]
                
               either (trim copy line) = "" [
                       last-element: 'empty
                       display-html: "<div>&nbsp;</div>"
                ] [
                    if not find [line empty] last-element  [
                        insert-missing-preceding-line
                        last-element: 'line
                    ]    
                    
                    if first-text-line = "" [first-text-line: join (take-left line 60) "..."]

                    
                    display-html:  rejoin [{<div class="} page-scheme {">} (apply-citation-set (markup-escape line) hotlinks monospace-style) "</div>"]
                    
                    

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
either (1 < length? table-of-contents ) [
    table-of-contents-string: rejoin [
        {<div id=toc-container>} newline
        newline
        {<div id=toc-label>Contents</div>}  newline
        (rejoin table-of-contents) newline
        {</div>} newline
        newline
    ]
    
    navigation-container-class: "navigation-container-toc"
] [
    navigation-container-class: "navigation-container-no-toc"

]
    


if  show-web-header and ((page-scheme = 'http) or (page-scheme = 'https)) [
    insert head out-string rejoin [
        {<div style="font-size:small;margin-bottom:1em;text-align:center;margin-left:3em; width:450px">
                
                    <i>Simplified web page with reduced interactivity and tracking. <br>
                        Some content may not be visible.</i>
        
        <table cellpadding=0 cell-spacing=0 
            style="width:80%;text-align:center;margin-left:auto; margin-right:auto;margin-bottom:1em;">
            <tr>
                <td align=center width=25% valign=top style=font-size:small>
                    <a 
                        style=text-decoration:none
                        class=other
                        title="View main page text only" 
                        id="web-switch-plain"
                        href="} uri  {"> <i><nobr>plain text</nobr></i></a>
                </td>
                <td align=center width=20% valign=top style=font-size:small>
                    <a 
                        style=text-decoration:none
                        class=other
                        id="web-switch-simplified"
                        title="View simplified main content only, with some links and headings" 
                        href="} uri  {"><i> simplified</i></a>
                </td>
                <td align=center width=20% valign=top style=font-size:small>
                    <a 
                        style=text-decoration:none
                        class=other
                        id="web-switch-all"
                        title="Verbose view of all content of the web page, including any navigational boilerplate links and content" 
                        href="} uri  {"><i> verbose</i></a>
                </td>
                <td align=center width=35% valign=top style=font-size:small>
                    <a 
                        style=text-decoration:none
                        class=other
                        id="web-launch-external"
                        title="View full content using system web browser"
                        href="} uri  {"><i><nobr>launch browser</nobr></i></a>
                </td>
            </tr>

        </table>
        
        <hr noshade style=align:center;width:80%;height:1px>
        </div>
        }
    ]
]
    
    
;--save the content to a HTML file
;--theme html should be UTF-8 charset, which is the standard format.
theme-html: read/string to-file rejoin [theme ".htm"]
theme-css: read/string to-file rejoin [theme ".css"]


;populate the theme
replace/all theme-html "{{scheme}}" page-scheme
replace/all theme-html "{{title}}" page-title
replace/all theme-html "{{theme-css}}" theme-css
replace/all theme-html "{{table-of-contents}}" table-of-contents-string
replace/all theme-html "{{navigation-container-class}}" navigation-container-class
replace/all theme-html "{{site-id}}" site-id
replace/all theme-html "{{site-id-md5}}" uri-md5
replace/all theme-html "{{site-id-md5-reversed}}" (reverse copy uri-md5)
replace/all theme-html "{{identicon-image}}" identicon-image
replace/all theme-html "{{fabric-image}}" fabric-image
replace/all theme-html "{{content}}" out-string

;save to final location
write out-path  theme-html
