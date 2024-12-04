/*
 * Round x to y decimal places
 */
const round = function (x, y) {
  const multiplier = Math.pow(10, y);
  return Math.round((x * multiplier).toFixed(1)) / multiplier;
};


/*
 * Give suggestions for links to issues or comments when typing a # in a textarea.
 */
$.fn.textAreaSuggestion = function () {
  var textAreaSuggestionBox = $('<ul class="suggestion-box"></ul>').appendTo('body').hide();
  return this.each(function () {
    var textarea = $(this);
    textarea.keydown(function (event) {
      if (event.key == 'Shift' || event.key == 'Ctrl') {
        return;
      }
      var textarea = $(this);
      var previousdata = textarea.data('query') == undefined ? '' : textarea.data('query');
      if (textarea.data('continue') == true && (/^[a-z0-9]{1}$/i.test(event.key) || event.key == '#')) {
        event.preventDefault();
        textarea.data('query', previousdata + event.key);
        getSuggestions(textarea, textAreaSuggestionBox, textarea.data('query'));
        textarea.data('continue', true);
      } else if (event.key == '#') {
        getSuggestions(textarea, textAreaSuggestionBox);
        textarea.data('continue', true);
      } else if (textarea.data('continue') && event.key == 'Backspace' && textarea.data('query').length > 0) {
        event.preventDefault();
        textarea.data('query', previousdata.substring(0, previousdata.length - 1));
        getSuggestions(textarea, textAreaSuggestionBox, textarea.data('query'));
        textarea.data('continue', true);
      } else if (textarea.data('continue') && (event.key == 'Tab' || event.key == 'Enter')) {
        event.preventDefault();
        var link = textAreaSuggestionBox.find('li:not(.query)').first().data('link');
        textarea.insert(link);
        textAreaSuggestionBox.hide();
        textarea.focus();
        textarea.data('continue', false);
        textarea.data('query', '');
      } else {
        textAreaSuggestionBox.hide();
        textarea.data('continue', false);
        textarea.data('query', '');
      }
    });
  });
};

var getSuggestions = function (textarea, textAreaSuggestionBox, query) {
  if (query == undefined) query = '';
  var projectId = textarea.data('suggestions-project');
  var caret = getCaretCoordinates(textarea[0], textarea[0].selectionEnd);
  var top = caret.top + textarea.offset().top;
  var left = caret.left + textarea.offset().left;
  $.ajax({
    url: '/issue/suggestions?projectId=' + projectId + '&query=' + query,
    success: function(data) {
      textAreaSuggestionBox.show().html('').css('top', top + 'px').css('left', left + 'px');
      var queryLi = $('<li class="query">query: ' + query + '</li>').appendTo(textAreaSuggestionBox);
      for (var i in data) {
        var suggestion = data[i];
        var item = $('<li class="' + suggestion.extraValue + '">' + suggestion.value + '</li>');
        item.data('link', suggestion.data);
        item.click(function() {
          textarea.insert($(this).data('link') + ' ');
          textAreaSuggestionBox.hide();
          textarea.focus();
        });
        textAreaSuggestionBox.append(item);
      }
    }
  });
};

/*
 * Highlight elements that are hash linked to.
 */
var hightlightHash = function() {
  if (window.location.hash) {
    $(window.location.hash).addClass('highlight').on('animationend', function() {
      $(this).removeClass('highlight');
    });
  }
};
window.onhashchange = hightlightHash;

/*
 * Convert value from current elemente into slug and put it in target element [data-slugify]
 */
$.fn.slugify = function () {
  return this.each(function() {
    $(this).change(function () {
      var target = $($(this).data('slugify'));
      $.ajax({
        method: 'POST',
        data: {
          text: $(this).val()
        },
        url: '/slug/index',
        success: function (data) {
          target.val(data);
        }
      });

    }).change();
  });
};

/*
 * Insert text string into textarea at cursor
 */
$.fn.insert = function (text) {
  return this.each(function () {
    var textarea = $(this);
    if (textarea[0].nodeName === "TEXTAREA") {
      textarea.val(textarea.val().substr(0, textarea.prop("selectionStart")) + text + textarea.val().substr(textarea.prop("selectionEnd")));
    }
  });
};

/*
 * Replace all dots into commas
 */
$.fn.commafy = function() {
  return this.replace('.', ',');
};

/*
 * Replace all commas into dots
 */
$.fn.dotify = function() {
  return this.replace(',', '.');
};

$.fn.currency = function() {
  return round(parseFloat(this), 2).commafy();
};

/*
 * Show given error below element
 */
$.fn.error = function(error) {
  return this.each(function () {
    $(this).siblings('small.error').remove();
    $(this).after('<small class="error">' + error + '</small>').parent().addClass("error");
  });
}

/*
 * Make given field remember its value by cookies
 */
$.fn.cookieRemember = function() {
  return this.each(function() {
    var name = $(this).data('remember');
    if ($.cookie(name) && self.queryString['query'] == undefined) {
      $(this).val($.cookie(name)).parents('form').submit();
    }
    $.cookie(name, $(this).val());
  }).change(function() {
    var name = $(this).data('remember');
    $.cookie(name, $(this).val());
  });
};

/*
 * Set Ctrl + S to save the forms on the page
 */
$.fn.ctrlS = function() {
  return this.keydown(function(e) {
    if (!(e.which == 83 && e.ctrlKey)) return true;
    $("form").submit();
    e.preventDefault();
    return false;
  });
};

/*
 * Make rows in a table clickable (gets the first link found in the row)
 */
$.fn.tableRowClickable = function() {
  return this.each(function() {
    $(this).find('tr').click(function() {
      var href = $(this).find("a").first().attr("href");
      if (href) {
        window.location = href;
      }
    });
  });
};

/*
 * Make a link ask for confirmation before following link
 */
$.fn.confirm = function() {
  return this.each(function() {
    $(this).click(function() {
      var message = $(this).data('confirm');
      return confirm(message);
    });
  });
};


/*
 * Markdownify the contents of the element
 */
$.fn.markdownify = function() {
  marked.setOptions({
    breaks: true
  });
  return this.each(function() {
    $(this).html(marked($(this).html()));
  });
};

var queryString = [];
var match,
        pl = /\+/g,  // Regex for replacing addition symbol with a space
        search = /([^&=]+)=?([^&]*)/g,
        decode = function (s) { return decodeURIComponent(s.replace(pl, " ")); },
        query = window.location.search.substring(1);

while (match = search.exec(query))
  queryString[decode(match[1])] = decode(match[2]);