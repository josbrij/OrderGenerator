OrderGenerator

O OrderGenerator é a aplicação web usada para preencher e enviar ordens. Ela foi criada com ASP.NET Core Razor Pages e se comunica com a exchange usando FIX 4.4 e QuickFIX.

Fluxo da ordem

O OrderGenerator deve apresentar numa página web um formulário com os seguintes campos, com os quais será criada uma nova ordem (NewOrderSingle):

• Símbolo: escolhido entre PETR4, VALE3 ou VIIA4.

• Lado: escolhido entre Compra ou Venda.

• Quantidade: valor positivo inteiro menor que 100.000.

• Preço: valor positivo decimal múltiplo de 0.01 e menor que 1.000.

No resultado do formulário, apresentar a resposta da requisição.
O formulário oferece PETR4, VALE3 e VIIA4; compra ou venda; quantidade inteira de 1 a 99.999; e preço de R$ 0,01 a R$ 999,99, em centavos.



Para executar localmente requer o SDK do .NET 8 e acesso ao NuGet para restaurar os pacotes.

Com o perfil HTTP padrão, a página fica em <http://localhost:5124>.

## Configuracao FIX

O arquivo [fix-initiator.cfg](OrderGenerator/fix-initiator.cfg) configura uma sessão FIX 4.4:

- SenderCompID: `ORDERGENERATOR`
- TargetCompID: `ORDERACCUMULATOR`
- Host: `127.0.0.1`
- Porta TCP: `5001`

- Log de mensagens FIX é gravado no arquivo fix-acceptor.log na pasta de saída.
