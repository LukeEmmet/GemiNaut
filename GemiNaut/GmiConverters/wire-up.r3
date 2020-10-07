REBOL [
    title: "citation wireup"
]






extract-citation: funct [line uri-object] [
        whitespace: [" " | "^-"]
        digit: charset [#"0" - #"9"]
        
        index-pattern:    ["[" [some digit] "]"]   
        if "=> " =  take-left line 3 [
            content: trim take-from line 4
            
            if content <> "" [
                either (parse content [copy url to whitespace copy display to end]) [
                    trim url
                    trim display
                ] [
                    url: content
                    display: content
                ]
                
            ]

            
            url: build-link uri-object  url
            
            ;--does the display text match "[nn] display text", if so add to citations list
            return any [
            
                 if (parse display [index-pattern to whitespace copy link-text to end]) [
                    index: take-left display ((length? display) - (length? link-text))
                    key: index

                    ;citations/:key: url
                     reduce [index url link-text]
                ] 
                
                if (parse display [index-pattern copy link-text to end]) [
                    index: take-left display ((length? display) - (length? link-text))
                    reduce [index url ""]
                ]

            
                none
            ]
            
            
            
        ]
]



get-citations: funct [lines uri-object] [
    
    citations: make map! []
    
    foreach line lines [
        link-candidate: extract-citation line uri-object
        if not none? link-candidate [
            key: link-candidate/1
            citations/:key: link-candidate/2
            
        ]
    ]
    :citations
]


link-wirer: funct [span-content hotlinks as-monospace] [ 
    
   either (error? try [index: to-integer span-content]) [
         rejoin ["[" span-content "]"]
    ] [
        
        citation: rejoin [ "[" span-content "]"]
        
        either ( none? hotlinks/citations/:citation) [
            rejoin ["[" span-content "]"]
        ] [
            hotlinks/replacements/:citation: true
            ;citations/:citation
            rejoin [
                (either as-monospace [""] [{<sup>}])
                {<a }
                       { style=text-decoration:none;font-size:0.95em }
                    { title="} hotlinks/citations/:citation {"}
                    { href="} hotlinks/citations/:citation {"}
                    {>}  "[" span-content "]" {</a>}
                (either as-monospace [""] [{</sup>}])
            ]
        ]
    ]
]




;---expands numeric square bracket citations using a handler that operates on their 
;---content. E.g. [nn] -> [F(nn)]
expand-citations: funct [text hotlinks handler as-monospace] [
    out: copy ""
    buffer: copy ""
    to-buffer: off

    digit: charset [#"0" - #"9"]

    foreach char text [
        any [
            if (char = #"[") [
               append buffer char
               to-buffer: on
                true
            ]
            
            if (char = #"]") [
                buffer-content: copy next buffer
                
                either (parse next buffer [some digit]) [
                    append out rejoin [ (handler buffer-content hotlinks as-monospace) ]
                ] [
                    append out join buffer char
                ]
                
                to-buffer: off
                buffer: copy ""
                true
            ]
            
            ;--default
           if true [
              either to-buffer [append buffer char] [append out char]
              true
            ]
        ]
    ]

    ;---emit anything still in the buffer
    append out buffer
    
    :out
]

apply-citation-set: funct [line hotlinks as-monospace] [
    either hotlinks/expand-citations [
        expand-citations line hotlinks :link-wirer as-monospace
    ] [
        line
    ]
]





;====================TESTING ==================

test-wireup: does [


;--load these for testing - otherwise they are already loaded for normal use
do load %utils.r3
do load %link-builder.r3

hotlinks: context [
    replacements: make map! []      ;returns none if entry is missing
    citations: copy []
]

data: {

# a heading

a paragraph with a citation [1] and another citation [100] 

and again [2] that is good

* foo [4]

=> 4: foo
=> stuff [334]
=> gemini://url/1 [1] citation 
=> bar/baz [2] citation 
=> gemini://url/100 [100] citation 
=> http://dont/link ignore this one

}

lines: (parse/all data "^/")
;lines: read/lines to-rebol-file "C:\Users\lukee\AppData\Local\Temp\geminaut_dygxdwi5.h22\1dd168d155f3f47953b09ef677daa8ea.gmi"
citations:  get-citations  lines (decode-url "http://foo/bar")
probe citations
foreach line lines [
    probe apply-citation-set line hotlinks
]


]


;=============
;test-wireup
            

            
