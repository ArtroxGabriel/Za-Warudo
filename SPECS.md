# CK0117 - Sistemas de Bancos de Dados - 2025-1

```markdown
Javam Machado, Antonio Marreiras, Gabriel Magalhães
```

## TRABALHO III - Controle de Concorrência

### Escalonador baseado em Timestamp

---

## 1. Aspectos Gerais

Este trabalho tem como objetivo verificar se um determinado escalonamento de transações
em um sistema de banco de dados é _serializável_, ou seja, se sua execução produz
o mesmo resultado que uma execução serial (não-concorrente) das mesmas transações.

Para isso, será utilizado o algoritmo **Escalonador Baseado em Timestamp**
(_Timestamp-Based Scheduling_), que assegura a serialização por meio da atribuição
de timestamps únicos para cada transação. Essas marcas definem uma ordem de precedência,
garantindo que as operações de leitura e escrita sejam executadas de forma equivalente
a um escalonamento serial, mesmo em ambientes concorrentes.

## 2. Implementação

Cada equipe de, no máximo, **dois alunos**, conforme definido na tabela de equipes
do Trabalho II, deverá implementar, em linguagem **C, C++ ou C#**, um código capaz
de:

1. Ler escalonamentos de transações a partir de um único arquivo de entrada `in.txt`.
2. Verificar se o escalonamento é serializável, utilizando o algoritmo Escalonador
   Baseado em Timestamp.
3. Gerar um arquivo de log, nomeado `out.txt`, contendo:
    - A conclusão se o escalonamento é serial (_serializável_).
    - Caso contrário, registrar uma marcação **"ROLLBACK"** de re-inicialização
      da transação, juntamente com o seu **"momento"** conforme definido abaixo.

O **momento** de uma operação é definido como a contagem sequencial, iniciando em
zero, que inclui todas as operações realizadas até então, inclusive a própria operação
em questão.

O arquivo `in.txt` segue a seguinte estrutura:

```txt
# Objeto de dados utilizados;
# Transações;
# Timestamps;
# Escalonamentos

X, Y, Z;
t1, t2, t3;
5, 10, 3;

E_1-r1(X) r2(Y) w2(Y) r3(Y) w1(X) c1
E_2-w2(X) r1(Y) w3(X) r2(Z) w1(Z) c1
E_3-r3(X) w3(Y) c1 r1(X) w1(Y) c2 r2(Y) w2(Z) c3
```

Resultado esperado no `out.txt`:

```txt
E_1-ROLLBACK-3
E_2-ROLLBACK-2
E_3-OK
```

A dupla deverá criar **arquivos para cada objeto de dado** e salvar nesses arquivos
as seguintes informações cada vez que o respectivo objeto for utilizado por alguma
operação:

1. O identificador do escalonamento (E_1, E_2, ...);
2. Qual a operação feita sobre o objeto (_Read_, _Write_);
3. Em que momento do escalonamento a operação foi realizada.

Além disso, deve ser criada e gerenciada a seguinte estrutura de dados durante a
verificação de um escalonamento:

```txt
<ID-dado, TS-Read, TS-Write>
```

Essa estrutura deve ser reinicializada a cada novo escalonamento.

## 3. Entrega

**Data da entrega:** Sexta-feira, **25 de julho de 2025 até as 10h00**, com apresentação
e arguição no **LEC/DC**, no horário da aula.

O código do programa e os arquivos de resultado devem ser enviados pelo **Classroom**
até o final do horário de entrega. Envios posteriores serão penalizados.

Dúvidas podem ser encaminhadas aos monitores:

- Gabriel Magalhães: [gabriel.alves@lsbd.ufc.br](mailto:gabriel.alves@lsbd.ufc.br)
- Antonio Alves: [antonio.marreiras@lsbd.ufc.br](mailto:antonio.marreiras@lsbd.ufc.br)
