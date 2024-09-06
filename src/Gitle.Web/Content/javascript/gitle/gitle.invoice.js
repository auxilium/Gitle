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
    var linesPrice = 0.0;
    $('.invoiceline').each(function () {
      var linePrice = parseFloat($(this).find('.invoiceline-price').val().replace(',', '.')) * (1 - parseInt($(this).find('.invoiceline-null').val()));
        linesPrice += linePrice;
    });
    var correctionLinesPrice = 0.0;
    $('.correctionline').each(function () {
      var correctionValue = $(this).find('.correctionline-price').val();
      if (correctionValue)
          correctionLinesPrice += parseFloat(correctionValue.replace(',', '.'));
    });
    subtotalPrice = linesPrice + correctionLinesPrice;
    $('#invoice_Subtotal').val(subtotalPrice.toFixed(2).toString().replace(".", ","));
    var vat = parseInt($('.vatline .vatline-vat').val());
    var vatPrice = subtotalPrice * vat * 0.21;
    $('.vatline-price').val(vatPrice.toFixed(2).toString().toString().replace(".", ","));
    var totalPrice = subtotalPrice + vatPrice;
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
      //line.find('.vatline-price').removeClass('null');
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
  $('.edit-invoice').each(function () {    
    var price = parseFloat($(this).find('.correctionline-price').val());

    if (price > 0) {
      $(this).show();
    } else {
      $(this).hide();
    }
  });

  $('.correctionline').find('.correctionline-price').change(function () {
    if ($(this).find('.correctionline-price').val() != "") {
      $(this).parents('.correctionline').next('.correctionline').show();
    }
  });

  $('.correctionline').find('.correctionline-price').keydown(function (e) {
    if (e.key === "Tab" || e.keyCode === 9 || e.which === 13) {
        e.preventDefault();
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