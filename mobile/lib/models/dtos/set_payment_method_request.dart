// Summary: Defines the request payload used to set a ticket's payment method.
// lib/models/dtos/set_payment_method_request.dart

class SetPaymentMethodRequest {
  final String paymentMethod;

  const SetPaymentMethodRequest({required this.paymentMethod});

  Map<String, dynamic> toJson() => {
        'paymentMethod': paymentMethod,
      };
}