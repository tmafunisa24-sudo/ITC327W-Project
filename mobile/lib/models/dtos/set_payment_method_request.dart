// lib/models/dtos/set_payment_method_request.dart

class SetPaymentMethodRequest {
  final String paymentMethod;

  const SetPaymentMethodRequest({required this.paymentMethod});

  Map<String, dynamic> toJson() => {
        'paymentMethod': paymentMethod,
      };
}