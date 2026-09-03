// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../../nameserver/preclude/Preclude.sol";

import '@openzeppelin/contracts/token/ERC20/ERC20.sol';
import "./ERC20NormalInterface.sol";

contract ERC20Normal is ERC20, ERC20NormalInterface {

    using SafeMath for uint256;
    using EnumerableSet for EnumerableSet.AddressSet;

    address internal _owner;

    EnumerableSet.AddressSet internal _managers;

    constructor(
        string memory _name_,
        string memory _symbol_
    )
    ERC20(
        _name_,
        _symbol_
    ){
        _owner = msg.sender;
        _managers.add(msg.sender);
    }

    modifier onlyOwner() {
        require(_owner == msg.sender, "onlyOwner");
        _;
    }

    modifier onlyAuth() {
        require(
            _owner == msg.sender ||
            _managers.contains(msg.sender),
            "onlyAuth"
        );
        _;
    }

    function mint(address account, uint256 amount) override external onlyAuth {
        _mint(account, amount);
    }

    function burn(address account, uint256 amount) override external onlyAuth {
        _burn(account, amount);
    }

    function managers() override view public returns (address[] memory) {
        address[] memory ret = new address[](_managers.length());
        for (uint256 i = 0; i < _managers.length(); i++) {
            ret[i] = _managers.at(i);
        }
        return ret;
    }

    function setManager(address[] memory who, bool[] memory add) override external onlyOwner {
        for (uint256 i = 0; i < who.length; i++) {
            if (add[i]) {
                _managers.add(who[i]);
            } else {
                _managers.remove(who[i]);
            }
        }
    }

    function transferOwnership(address newOwner) override public onlyAuth {
        require(newOwner != address(0), "new owner is the zero address");
        emit OwnershipTransferred(_owner, newOwner);
        _owner = newOwner;
    }

    function _beforeTokenTransfer(address from, address to, uint256 amount) virtual override(ERC20) internal {

        ERC20._beforeTokenTransfer(from, to, amount);
    }
}
