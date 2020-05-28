# GemiNaut

A user friendly client to browse Gemini from Windows.

Gemini is a radically simple protocol and text format for browsing on the Internet.
For more information see <a href="https://gemini.circumlunar.space/">https://gemini.circumlunar.space/</a>

# Download

You can download a pre-built application for Windows from the GemiNaut home page: 
<a href="https://www.marmaladefoo.com/pages/geminaut">https://www.marmaladefoo.com/pages/geminaut</a>

# Key features

* Familiar navigation actions, smooth scrolling and text wrap
* flexible theming engine
* Site based themes
* Visually distinguish gemini from other links
* View source

# License 

GPL 3

# History

* added simple userguide
* infer the document title from the first heading or para text line and show in window caption
* darken the background a tiny bit in Fabric theme to be less saturated
* workaround for GemGet bug which overwrites into the output file, rather than replacing the whole file
* user selectable themes (4 to start with Fabric, Plain, Terminal and Unified UI)
* html escape source content before display
* pass torture tests relating to link formation 
* detect response redirect urls and adjust links accordingly
  (e.g. gemini://gemini.circumlunar.space/users/solderpunk -> 
   gemini://gemini.circumlunar.space/users/solderpunk/)
* pad output with blank lines at end for better display of short content
* new plain line blocks always preceded by at least one blank line
* prettify spacing of headings, always have a blank line before these.
* prettify spacing of links/bullets, always have a blank line unless previous element was one too
   (e.g. gemini://gemini.circumlunar.space:1965/users/acdw/ is laid out more pleasantly)
* when loading raw gmi for view source get browser to interpret as text/plain 
* application icon from http://www.iconarchive.com/show/pretty-office-8-icons-by-custom-icon-design/Text-align-left-icon.html
* txtUrl follows page better
* local versions of Rebol and GmiConverters and Gemget folders used if found
* more pretty handling of page not found (status 51)
* txtUrl never shows https urls
* user can edit home page
* show prompt for query building
* decorate links to expected binary files with document glyph to hint content
* hanging indent on bullets and links
* visual hinting of non gemini links with glyph and link style
* show tooltip of url to be navigated to
* code fences for preformatted areas, including label as tooltip
* toast popups for error conditions
* session and server independent identikons and page background texture
* user based visual identity/theme for personal sub-sites, not requiring author control
* view source
* launch external urls in system browser
* use http proxy for common binary file types
* empty cache on close
* prettify links, headings and bullets


# Free in virture of using web browser

* go back and forward remembers scroll offset
* rich CSS styling and visual design
* smooth scrolling
* tab through page links
* cursor keys, page/up down, keyboard shortcuts for back/forwards
* zoom with wheelmouse
* navigate forwards, back, quickly (browser cache)
* wrap long lines to window
* Ctrl+F to find in current page
* Select all, copy to clipboard
* Ctrl+P to print page

