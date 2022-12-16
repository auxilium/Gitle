/*
 * input element that needs to parse its value into something of a readable time, writes the number of minutes to [data-minutes-field]
 */
$.fn.bookingParser = function () {
  return this.change(function () {
    var bookingInput = $($(this).data('minutes-field'));
    var value = $(this).val().replace(',', '.');

    var timeRegex = /^([0-9]+):([0-9]{2})$/i;
    var decimalRegex = /^([0-9]+)\.([0-9]+)$/i;
    var numberRegex = /^[0-9]+$/i;

    var visualOutput = value;
    var hoursOutput = value;

    if (numberRegex.test(value)) {
      var number = parseInt(value);
      if (number > 9 && number <= 60) {
        var fullhours = parseInt(number / 60);
        var fullminutes = number - (fullhours * 60);
        visualOutput = fullhours + ':' + (fullminutes < 10 ? "0" + fullminutes : fullminutes);
        hoursOutput = number;
      } else {
        visualOutput = number + ':00';
        hoursOutput = number * 60;
      }
    }
    if (timeRegex.test(value)) {
      var parts = value.split(':');
      var hours = parseFloat(parts[0]);
      var minutes = parseFloat(parts[1]);
      hoursOutput = (hours * 60 + minutes);
    }
    if (decimalRegex.test(value)) {
      var hours = parseFloat(value);
      hoursOutput = (hours * 60);
      var fullhours = parseInt(value);
      var fullminutes = (hours - fullhours) * 60;
      visualOutput = fullhours + ':' + (fullminutes < 10 ? "0" + fullminutes : fullminutes);
    }

    $(this).val(visualOutput);
    bookingInput.val(hoursOutput);
    bookingInput.trigger('change');
  });
};

/*
 * Initializes the element with Gitle search functionality
 */
$.fn.gitleSearch = function () {
  return this.each(function () {
    var searchField = $(this);
    searchField.autocomplete({
      serviceUrl: '/search',
      autoSelectFirst: true,
      onSelect: function (suggestion) {
        window.location = suggestion.data;
      }
    });

    $(document).keydown(function (e) {
      if (e.which == 70 && e.ctrlKey && e.shiftKey) {
        searchField.focus();
      }
    });

  });
};

/*
 * Initializes the textarea as a gitle live comments textarea
 */
$.fn.liveComments = function () {
  return this.each(function () {
    var url = $(this).data('live-comments');
    var textarea = $(this);
    var staticComments = $('<div>').addClass('comments').html(marked(textarea.val()));
    var container = $('<div>').addClass('comments-container').insertAfter(textarea).append(staticComments).append(textarea);
    staticComments.click(function () {
      container.addClass('edit');
      textarea.focus();
    });
    textarea.blur(function () {
      $.ajax({
        url: url,
        method: 'POST',
        dataType: 'text',
        data: {
          comment: textarea.val()
        },
        success: function (data) {
          staticComments.html(marked(data));
          container.removeClass('edit');
        },
        error: function () {
          console.log('niet opgeslagen');
        }
      });
    });
  });
};

$.fn.startEndDatePreset = function () {
  return this.click(function () {
    $($(this).data('insert-startdate-to')).val($(this).data('insert-startdate'));
    $($(this).data('insert-enddate-to')).val($(this).data('insert-enddate'));
  });
}

$.fn.initProjectTypeNumbers = function () {
  return this.change(function () {
    var numberField = $($(this).data('numberfield'));

    var value = $(this).val();

    switch (value) {
      case "1":
        numberField.val(numberField.data('initial-number'));
        break;
      case "2":
      case "4":
        numberField.val(numberField.data('service-number'));
        break;
      case "3":
        numberField.val(numberField.data('internal-number'));
        break;
      default:
        numberField.val('');
    }

    $($(this).data('insert-startdate-to')).val($(this).data('insert-startdate'));
    $($(this).data('insert-enddate-to')).val($(this).data('insert-enddate'));
  });
}

$(function () {
  var activeRow = undefined;
  $('.booking-edit').on('click', function (e) {
    e.preventDefault();
    var row = $(this).parents('.booking-row');
    if (activeRow != undefined) {
      activeRow.find('.booking-row-edit').remove();
      activeRow.find('.booking-row-content').show();
    }
    activeRow = row;
    $.ajax({
      method: 'GET',
      data: {
        id: row.data('id')
      },
      url: '/booking/edit',
      success: function (data) {
        row.find('.booking-row-content').hide();
        row.prepend(data);
        addEditListeners(row);
        bookingRowInit(row);
      }
    });
  });

  $('.booking-row').on('click', function (e) {
    if ($(e.target).is('input[type="checkbox"], a')) return;
    e.stopPropagation();
    var selectBooking = $(this).find('.select-booking');
    selectBooking.prop('checked', !selectBooking.prop('checked'));
    selectBooking.trigger('change');
  });

  $('.select-booking').off('change').on('change', function () {
    $(this).parents('.booking-row-content').toggleClass('selected', $(this).prop('checked'));
    $('#move-bookings-date, #move-bookings-button').toggle($('.select-booking:checked').length > 0);
  });

  var addEditListeners = function (row) {
    row.find('.booking-edit-cancel').on('click', function (e) {
      e.preventDefault();
      row.find('.booking-row-edit').remove();
      row.find('.booking-row-content').show();
      activeRow = undefined;
    });
  };

  var datepickerOptions = {
    format: 'dd-mm-yyyy',
    weekStart: 1
  };

  var setUnbillable = function (row) {
    row.find('.booking-row-null-toggle').addClass('null');
    row.find('.booking_Unbillable').val(1);

  }

  var setBillable = function (row) {
    row.find('.booking-row-null-toggle').removeClass('null');
    row.find('.booking_Unbillable').val(0);
  }

  var bookingRowInit = function (row) {

    row.find('.project-chooser').data('suggestion', undefined);

    row.find('.project-chooser').autocomplete({
      serviceUrl: '/project/autocomplete',
      autoSelectFirst: true,
      noCache: true,
      minChars: 0,
      onSelect: function (suggestion) {
        var projectChooser = row.find('.project-chooser');
        if (projectChooser.data('suggestion') != undefined && projectChooser.data('suggestion') == suggestion.data) return false;
        projectChooser.data('suggestion', suggestion.data).val(suggestion.value);
        row.find('.booking_Project_Id').val(suggestion.data);
        row.find('.booking_Project_Slug').val(suggestion.extraValue3);
        row.find('.issue-chooser').val('').autocomplete('setOptions', { params: { projectId: suggestion.data } });
        row.find('.booking_Issue_Id').val('');
        if (suggestion.extraValue == "ticketRequired") {
          row.find('.issue-chooser').prop("required", true);
          $(document).foundation('abide', 'reflow');
        } else {
          row.find('.booking_Comment').prop("required", true);
          $(document).foundation('abide', 'reflow');
        }
        if (suggestion.extraValue2 == "unbillable") {
          setUnbillable(row);
        } else {
          setBillable(row);
        }
      }
    }).on('focus', function () {
      $(this).autocomplete().onValueChange();
    });;

    row.find('.issue-chooser').autocomplete({
      serviceUrl: '/issue/autocomplete',
      params: { projectId: row.find('.booking_Project_Id').val() },
      autoSelectFirst: true,
      noCache: true,
      minChars: 1,
      width: row.find('.project-chooser').width(),
      onSelect: function (suggestion) {
        row.find('.booking_Issue_Id').val(suggestion.data);
        row.find('.booking_Issue_Id').trigger('change');
        row.find('.issue-chooser').val(suggestion.value);
        if (suggestion.extraValue === 'Done') row.find('.issue-chooser').css('color','green');
        if (suggestion.extraValue === 'Closed') row.find('.issue-chooser').css('color','red');
        if (suggestion.extraValue === 'Hold') row.find('.issue-chooser').css('color','orange');
        row.find('.booking_Comment').prop("required", false);
        $(document).foundation('abide', 'reflow');
      },
      formatResult: function(suggestion, currentValue) {
        var result = $.Autocomplete.formatResult(suggestion, currentValue);
        if (suggestion.extraValue === 'Done') {
          result = '<div class="green">' + result + ' (uitgevoerd)</div>';
        }
        if (suggestion.extraValue === 'Closed') {
          result = '<div class="red">' + result + ' (gesloten)</div>';
        }
        if (suggestion.extraValue === 'Hold') {
          result = '<div class="orange">' + result + ' (on hold)</div>';
        }
        return result;
      }
    }).blur(function() {
      if ($(this).val() == '') {
        row.find('.booking_Issue_Id').val('');
      }
    });

    row.find('.booking-row-null-toggle').click(function (e) {
      e.preventDefault();
      if ($(this).hasClass('null')) {
        setBillable(row);
      } else {
        setUnbillable(row);
      }
    });

    row.find('.new-issue').click(function() {
      var projectSlug = row.find('.booking_Project_Slug').val();
      if (projectSlug == "") {
        return false;
      }
      $('#newIssue').foundation('reveal', 'open', {
        url: '/project/' + projectSlug + '/issue/new?cancelLayout=true',
        success: function (data) {
          setTimeout(function() {
            var gitleIssues = new GitleIssues();
            gitleIssues.initTimeParser();
            $('#newIssue').find('form').submit(function(e) {
              e.preventDefault();
              $.ajax({
                url: '/project/' + projectSlug + '/issue/0/ajaxsave',
                method: 'POST',
                data: $(this).serialize(),
                success: function(data) {
                  row.find('.booking_Issue_Id').val(data.Id).parent().removeClass('error');
                  row.find('.issue-chooser').val('#' + data.Number + ' - ' + data.Name);
                  row.find('.booking_Comment').prop("required", false).focus();
                  $(document).foundation('abide', 'reflow');
                  $('#newIssue').foundation('reveal', 'close');
                }
              });
            });
            $('#newIssue').find('#back-button').on('click', function(e) {
              e.preventDefault();
              $('#newIssue').foundation('reveal', 'close');
            });
          }, 100);
        }
      }).on('opened.fndtn.reveal', function() {
        $(this).find('[name="item.Name"]').focus();
      });
    });

    row.find('.date').fdatepicker(datepickerOptions);

    row.find('.booking-parser').bookingParser();

  };

  bookingRowInit($('.booking-row-new'));

  $('.date').fdatepicker(datepickerOptions).on('show', function () {
    $('[data-dayshift]').removeClass('active');
  });

  $('.reportdate').fdatepicker({
    format: 'yyyy-mm-dd',
    weekStart: 1
  }).on('changeDate, change', function (ev) {
    var query = $('#query').val();
    var target = $(ev.target);
    var prefix = target.data('filter-prefix');
    var rx = new RegExp(prefix + ":[a-zA-Z0-9-_,.]+");
    query = query.replace(rx, '');
    query = query + ' ' + prefix + ':' + target.val();
    $('#query').val(query.replace(/ +(?= )/g, ''));
    $('#query-form').submit();
  });



  var dayShiftFormat = 'dd MMM';
  if (new FoundationHelper().getCurrentSizeClass() === 'large') {
    dayShiftFormat = 'ddd dd MMM';
  }

  $('[data-dayshift]').each(function () {
    var shift = $(this).data('dayshift');
    var label = '';
    var date = Date.today().add(shift).days();
    if (date.toString('dd-MM-yyyy') === $('#booking_Date').val()) {
      $(this).addClass('active');
    }
    switch (shift) {
      case 0:
        label = 'Vandaag';
        break;
      case -1:
        label = 'Gisteren';
        break;
      default:
        label = date.toString(dayShiftFormat);
    }
    $(this).text(label);
  }).click(function (e) {
    e.preventDefault();
    var shift = $(this).data('dayshift');
    var date = Date.today().add(shift).days();
    $('#booking_Date').val(date.toString('dd-MM-yyyy')).change();
    //$('#booking_Date').val(date.toString('dd-MM-yyyy'));
    //$('[data-dayshift]').removeClass('active');
    //$(this).addClass('active');
  });

  $('#booking_Date').change(function () {
    window.location = '/Booking/index?date=' + $(this).val().toString('dd-MM-yyyy');
  });
});

$('.filter-select').prepend('<li><input type="text" class="filter-field" style="margin-bottom:0;"></li>').each(function () {
  var filterselect = $(this);
  var filterfield = filterselect.find('.filter-field');
  filterfield.attr('placeholder', 'Filter');

  filterfield.keyup(function () {

    filterselect.find('a')
      .each(function () {
        if (filterfield.val().length > 0 && !($(this).hasClass('remove-filter')) && !($(this).html().toLowerCase().indexOf(filterfield.val().toLowerCase()) > 0)) {
          $(this).parent('li').hide();
        } else {
          $(this).parent('li').show();
        }
      });
  });
});

$('.dropdown').each(function () {
  $(this).next().on('opened.fndtn.dropdown', function (e) {
    var filterfield = $(this).find('.filter-field').val('').trigger('keyup');
    setTimeout(function () {
      filterfield.focus();
    }, 50);
  });
});



$(function () {
  var computeHoursForIssue = function (issueNumber) {
    var issueLine = $('.invoiceline[data-issue=' + issueNumber + ']');
    var hoursButton = issueLine.find('.invoiceline-hours');
    var hours = parseFloat(hoursButton.text());
    var estimateButton = issueLine.find('.invoiceline-estimate');
    var estimate = parseFloat(estimateButton.text());
    if (hours < estimate) {
      estimateButton.addClass('success');
      hoursButton.addClass('alert');
    } else {
      estimateButton.addClass('alert');
      hoursButton.addClass('success');
    }
  };

  var computePriceForInvoiceLine = function (invoiceLine) {
    var hourPrice = parseFloat($('#invoice_HourPrice').val().replace(',', '.'));
    var hours = parseFloat(invoiceLine.find('.invoiceline-hours-input').val().replace(',', '.'));
    var nill = parseInt(invoiceLine.find('.invoiceline-null').val());
    var price = hours * hourPrice * (1 - nill);
    invoiceLine.find('.invoiceline-price').val(price.toFixed(2).toString().replace(".", ","));
    calculateTotals();
  };

  var calculateTotals = function () {
    var subtotalPrice = 0.0;
    $('.invoiceline').each(function () {
      var linePrice = parseFloat($(this).find('.invoiceline-price').val().replace(',', '.')) * (1 - parseInt($(this).find('.invoiceline-null').val()));
      subtotalPrice += linePrice;
    });
    var correctionTotalPrice = 0.0;
    $('.correctionline').each(function () {
      var correctionValue = $(this).find('.correctionline-price').val();
      if (correctionValue)
        correctionTotalPrice += parseFloat(correctionValue.replace(',', '.'));
    });
    subtotalPrice += correctionTotalPrice;
    $('#invoice_Subtotal').val(subtotalPrice.toFixed(2).toString().replace(".", ","));
    var vat = parseInt($('.vatline .vatline-vat').val());
    var vatPrice = (subtotalPrice + correctionTotalPrice) * vat * 0.21;
    $('.vatline-price').val(vatPrice.toFixed(2).toString().toString().replace(".", ","));
    var totalPrice = subtotalPrice + correctionTotalPrice + vatPrice;
    $('#invoice_Total').val(totalPrice.toFixed(2).toString().replace(".", ","));
  };

  $('.invoiceline-issue').each(function () {
    computeHoursForIssue($(this).data('issue'));
  });

  $('#invoice_HourPrice').keyup(function () {
    $('.invoiceline').each(function () {
      computePriceForInvoiceLine($(this));
    });
  }).keyup();

  calculateTotals();

  $('.invoiceline-hours-input').on('change keyup', function () {
    $(this).data('changed', true);
    var invoiceLine = $(this).parents('.invoiceline');
    computePriceForInvoiceLine(invoiceLine);
    invoiceLine.find('.invoiceline-estimate, .invoiceline-hours').removeClass('active');
    var hours = parseFloat($(this).val().replace(',', '.'));
    var estimateHours = parseFloat(invoiceLine.find('.invoiceline-estimate').text().replace(',', '.'));
    var bookingHours = parseFloat(invoiceLine.find('.invoiceline-hours').text().replace(',', '.'));
    if (hours == estimateHours) {
      invoiceLine.find('.invoiceline-estimate').addClass('active');
    } else if (hours == bookingHours) {
      invoiceLine.find('.invoiceline-hours').addClass('active');
    }
  });

  $('.correctionline-price').keyup(function () {
    calculateTotals();
  });

  $('.invoiceline-estimate, .invoiceline-hours, .invoiceline-extrahours').click(function (e) {
    e.preventDefault();
    var estimate = $(this).text();
    var invoiceLine = $(this).parents('.invoiceline');
    invoiceLine.find('.invoiceline-hours-input').val(estimate).change();
    invoiceLine.find('.invoiceline-estimate, .invoiceline-hours, .invoiceline-extrahours').removeClass('active');
    $(this).addClass('active');
  });

  $('.remove-line').click(function (e) {
    e.preventDefault();
    var line = $(this).parents('.invoiceline');
    if (line.data('issue')) {
      $('.booking[data-issue=' + line.data('issue') + ']').remove();
    }
    line.remove();
    calculateTotals();
  });

  $('.invoiceline-null-toggle').click(function (e) {
    e.preventDefault();
    var line = $(this).parents('.invoiceline');
    if ($(this).hasClass('null')) {
      $(this).removeClass('null');
      line.find('.invoiceline-null').val(0);
      line.find('.invoiceline-price').removeClass('null');
    } else {
      $(this).addClass('null');
      line.find('.invoiceline-null').val(1);
      line.find('.invoiceline-price').addClass('null');
    }
    computePriceForInvoiceLine(line);
    calculateTotals();
  });

  $('.vatline-toggle').click(function (e) {
    e.preventDefault();
    var line = $(this).parents('.vatline');
    if ($(this).hasClass('vat')) {
      $(this).removeClass('vat');
      line.find('.vatline-vat').val(1);
      line.find('.vatline-price').removeClass('null');
    } else {
      $(this).addClass('vat');
      line.find('.vatline-vat').val(0);
      line.find('.vatline-price').addClass('null');
    }
    calculateTotals();
  });

  // All decimal input fields have a class named 'number'
  $('#createinvoice input.comma').each(function () {
    $(this).keypress(function (e) {
      // '46' is the keyCode for '.'
      if (e.keyCode == '46') {
        // IE
        if (document.selection) {
          var range = document.selection.createRange();
          range.text = ',';
          // Chrome + FF
        } else if (this.selectionStart || this.selectionStart == '0') {
          var start = this.selectionStart;
          var end = this.selectionEnd;

          $(this).val($(this).val().substring(0, start) + ','
            + $(this).val().substring(end, $(this).val().length));

          this.selectionStart = start + 1;
          this.selectionEnd = start + 1;
        } else {
          $(this).val($(this).val() + ',');
        }
        return false;
      }
    });
  });

  $('.correctionline').hide();
  $('.correctionline').first().show();
  $('.correctionline').find('.correctionline-price').change(function () {
    if ($(this).find('.correctionline-price').val() != "") {
      $(this).parents('.correctionline').next('.correctionline').show();
    }
  });


  $('[data-booking]').each(function () {
    var bookingRow = $(this);
    var bookingHours = parseFloat(bookingRow.find('.booking-hours').val().replace(',', '.'));
    var issueId = bookingRow.data('issue');
    var invoiceLine = $('.invoiceline-issue[data-issue=' + issueId + ']');
    var invoiceLineHoursInput = invoiceLine.find('.invoiceline-hours-input');
    bookingRow.find('.delete-booking').click(function (e) {
      e.preventDefault();
      bookingRow.remove();
      var newInvoiceLineHours = parseFloat(invoiceLineHoursInput.val().replace(',', '.')) - bookingHours;
      if (!invoiceLineHoursInput.data('changed')) invoiceLineHoursInput.val(newInvoiceLineHours.toString().replace('.', ','));
      if (bookingRow.data('billable') === 'True') {
        var invoiceLineHours = invoiceLine.find('.invoiceline-hours');
        invoiceLineHours.html((parseFloat(invoiceLineHours.text().replace(',', '.')) - bookingHours).toString().replace('.', ','));
        var invoiceLineExtraHours = invoiceLine.find('.invoiceline-extrahours');
        var extraHours = parseFloat(invoiceLineExtraHours.text().replace(',', '.')) - bookingHours;
        if (extraHours < 0) extraHours = 0;
        invoiceLineExtraHours.html(extraHours.toString().replace('.', ','));
      }
      computePriceForInvoiceLine(invoiceLine);
    });
  });
});
function GitleIssues() { }

GitleIssues.prototype = {
  init: function() {
    this.initFilters();
    this.initThreeStateChecker();
    this.initGroupActions();
    this.initQuickView();
    this.initBookingsChart();
    this.initTimeParser();
    this.bookingRowInit();
  },

  initFilters: function () {
    function escapeRegExp(string) {
      return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    $('a[data-filter], a[data-filter-clear]').click(function (e) {
      e.preventDefault();
      var filter = $(this).data('filter');
      var oppositeFilter = $(this).data('filter-opposite');
      var query = $('#query').val();
      if ($(this).is('[data-filter-clear]')) query = '';
      if (query.indexOf(oppositeFilter) != -1) {
        query = query.replace(oppositeFilter, "");
      }
      if (oppositeFilter != undefined && oppositeFilter.indexOf(',') !== -1 && oppositeFilter.indexOf(':') === -1) {
        var opposites = oppositeFilter.split(',');
        for (var i in opposites) {
          var rx = new RegExp(opposites[i] + ":[a-zA-Z0-9-_,.]+");
          query = query.replace(rx, '');
        }
      }
      var match = query.match('\\b(' + escapeRegExp(filter) + ')');
      if (match != null && query.indexOf(match[0]) != -1) {
        query = query.replace(new RegExp('\\b(' + escapeRegExp(filter) + ')'), "");
      } else {
        query = query + " " + filter;
      }
      $('#query').val(query.replace(/ +(?= )/g, ''));
      $('#query-form').submit();
    });

  },

  initThreeStateChecker: function() {
    $('.three-state-checker').each(function () {
      var checker = $(this);
      checker.append('<i class="fa fa-square-o none"></i><i class="fa fa-check-square-o all"></i><i class="fa fa-minus-square-o some"></i>');
      var selector = $(this).data('selector');
      var checkboxes = $(selector);
      checkboxes.click(function () {
        if (checkboxes.length == checkboxes.filter(':checked').length) {
          checker.attr('data-state', 'all');
        } else if (checkboxes.filter(':checked').length === 0) {
          checker.attr('data-state', 'none');
        } else {
          checker.attr('data-state', 'some');
        }
      });
    }).click(function () {
      var selector = $(this).data('selector');
      var state = $(this).attr('data-state');
      var checkboxes = $(selector);
      switch (state) {
        case "none":
          checkboxes.attr('checked', 'checked');
          $(this).attr('data-state', 'all');
          break;
        default:
          checkboxes.removeAttr('checked');
          $(this).attr('data-state', 'none');
          break;
      }
      checkboxes.change();
    });
  },

  initGroupActions: function() {
    $('#group-actions').hide();
    $('.issues input[type=checkbox][name=issue]').change(function () {
      var checkedBoxes = $('.issues input[type=checkbox][name=issue]:checked');
      if (checkedBoxes.length > 0) {
        $('#group-actions, #exportselection').show();
      } else {
        $('#group-actions, #exportselection').hide();
      }
      var values = [];
      checkedBoxes.each(function () {
        values.push($(this).val());
      });
      $('#group-actions [name=issues]').val(values.join(','));
    }).change();

    $('[data-group-action]').click(function (e) {
      e.preventDefault();
      var issues = $('#group-actions input[name=issues]').val();
      var href = $(this).prop('href');
      location.href = href + (href.indexOf('?') == -1 ? '?' : '&') + 'issues=' + issues;
    });

    $('.issue').click(function () {
      var checkbox = $(this).find('input[type=checkbox]');
      checkbox.prop('checked', !checkbox.is(':checked')).change();
    }).on('click', 'a, input', function (e) {
      e.stopPropagation();
    });
  },

  initQuickView: function() {
    var quickviewDelay = 800;
    $('[data-quickview]').hover(function () {
      var link = $(this);
      if ($(this).data('tooltip') == undefined) {
        var offset = $(this).position();
        var tooltip = $('<div class="quickview">').css('top', offset.top + $(this).height()).css('left', offset.left).css('z-index', 10);
        $(this).data('tooltip', tooltip);
        tooltip.load($(this).data('quickview'), function () {
          tooltip.find('.marked').each(function () {
            $(this).html(marked($(this).html()));
          });
          link.append(tooltip.hide());
          link.data('timeout', setTimeout(function () {
            if (link.is(':hover')) {
              link.data('tooltip').show();
            }
          }, quickviewDelay));
        });
      } else {
        link.data('timeout', setTimeout(function () {
          if (link.is(':hover')) {
            link.data('tooltip').show();
          }
        }, quickviewDelay));
      }
    }, function () {
      $('.quickview').hide();
    });
  },

  initBookingsChart: function () {
    var quickviewDelay = 0;
    $('[data-bookingschart]').hover(function () {
      var link = $(this);
      if ($(this).data('tooltip') == undefined) {
        var offset = $(this).position();
        var tooltip = $('<div class="bookingschart">').css('top', offset.top + $(this).height()).css('left', offset.left);
        $(this).data('tooltip', tooltip);
        tooltip.load($(this).data('bookingschart'), function () {
          tooltip.find('.marked').each(function () {
            $(this).html(marked($(this).html()));
          });
          link.append(tooltip.hide());
          link.data('timeout', setTimeout(function () {
            if (link.is(':hover')) {
              link.data('tooltip').show();
            }
          }, quickviewDelay));
        });
      } else {
        link.data('timeout', setTimeout(function () {
          if (link.is(':hover')) {
            link.data('tooltip').show();
          }
        }, quickviewDelay));
      }
    }, function () {
      $('.bookingschart').hide();
    });
  },

  initTimeParser: function() {
    $('.time-parser').blur(function () {
      var value = $(this).val();
      if (value === "" || value === "0") return;
      value = value.replace(/[A-Za-z$-]/g, "");
      value = value.replace(",", ".");
      value = parseFloat(value);
      var lessThanQuarter = value < 0.25;
      if (lessThanQuarter) {
        value = 0.25;
      }      
      $(this).val(value);
      if (lessThanQuarter) {
        $(this).error("minimum is een kwartier");
      }
    });
  },

  bookingRowInit: function() {
    var row = $('.project-chooser-issue').parent();

    row.find('.project-chooser-issue').data('suggestion', undefined);

    row.find('.project-chooser-issue').autocomplete({
      serviceUrl: '/project/autocomplete',
      autoSelectFirst: true,
      noCache: true,
      minChars: 0,
      onSelect: function(suggestion) {
        var projectChooser = row.find('.project-chooser-issue');
        if (projectChooser.data('suggestion') != undefined && projectChooser.data('suggestion') == suggestion.data) return false;
        projectChooser.data('suggestion', suggestion.data).val(suggestion.value);
        row.find('.issue_Project_Id').val(suggestion.data);
        row.find('.issue_Project_Slug').val(suggestion.extraValue3);
      }
    }).on('focus',
      function() {
        $(this).autocomplete().onValueChange();
      });;
  }

};

var gitleIssues = null;
$(function () {
  gitleIssues = new GitleIssues();
  gitleIssues.init();
});